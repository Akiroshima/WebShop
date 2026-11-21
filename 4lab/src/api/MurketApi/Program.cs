using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

using MurketApi.Api.Infrastructure;
using MurketApi.Application.Abstractions;
using MurketApi.Application.Services;
using MurketApi.Infrastructure;
using MurketApi.Infrastructure.Communication;
using MurketApi.Infrastructure.Metrics;
using MurketApi.Infrastructure.Persistence;
using MurketApi.Infrastructure.RateLimiting;
using MurketApi.Infrastructure.Security;
using MurketApi.Infrastructure.Seeding;

namespace MurketApi.Api;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var isDevelopment = builder.Environment.IsDevelopment();

        // Конфигурация — только переменные окружения (файлы appsettings*.json не используются;
        // источники по умолчанию, включая json-файлы, отключаются). Значения, уже подложенные
        // к моменту старта (WebApplicationFactory в тестах), сохраняются.
        var preset = builder.Configuration.AsEnumerable()
            .Where(pair => pair.Value is not null)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddEnvironmentVariables();
        builder.Configuration.AddInMemoryCollection(preset);

        var auth = AuthOptions.FromEnvironment(builder.Configuration, isDevelopment, builder.Environment);
        var database = DatabaseOptions.FromEnvironment(builder.Configuration, isDevelopment);
        var seed = SeedOptionsReader.FromEnvironment(builder.Configuration, isDevelopment, builder.Environment);

        // ----- DI: PostgreSQL, безопасность, сервисы -----
        builder.Services.AddSingleton(auth);
        builder.Services.AddSingleton(database);
        builder.Services.AddSingleton(seed);
        builder.Services.AddSingleton(NpgsqlDataSource.Create(database.ConnectionString));
        builder.Services.AddSingleton<Domain.Common.IClock, UtcClock>();
        builder.Services.AddSingleton<ISecretGenerator, CryptoSecretGenerator>();
        // Хэши секретов детерминированы от ключа развёртывания — refresh-токены из PostgreSQL
        // остаются действительными после перезапуска процесса (см. README, Auth__TokenHashSalt).
        builder.Services.AddSingleton<ITokenHasher>(new SaltedSha256TokenHasher(
            builder.Configuration["Auth__TokenHashSalt"] ?? auth.JwtKey));
        builder.Services.AddSingleton<IPasswordHasher>(provider =>
            new Pbkdf2PasswordHasher(auth.Pbkdf2Iterations, provider.GetRequiredService<IKdfCounter>()));
        builder.Services.AddSingleton<IAccessTokenService>(new JwtAccessTokenService(auth.JwtKey, auth.AccessTtl));
        builder.Services.AddSingleton<IRateLimiter, SlidingWindowRateLimiter>();
        builder.Services.AddSingleton<IKdfCounter, KdfCounter>();
        builder.Services.AddSingleton<KdfCounter>(provider => (KdfCounter)provider.GetRequiredService<IKdfCounter>());

        if (isDevelopment)
        {
            builder.Services.AddSingleton<IEmailSender, DevEmailSender>();
            builder.Services.AddSingleton<ISmsSender, DevSmsSender>();
        }
        else
        {
            builder.Services.AddSingleton<IEmailSender, SilentEmailSender>();
            builder.Services.AddSingleton<ISmsSender, SilentSmsSender>();
        }

        // ----- DI: хранилище (PostgreSQL) -----
        // Один запрос — одно соединение (scope): им совместно пользуются EF Core и репозиторий
        // заказов на «сыром» ADO.NET; единица работы открывает на нём транзакцию.
        builder.Services.AddScoped<PostgresStore>();
        builder.Services.AddDbContext<MurketDbContext>((provider, options) => options
            .UseNpgsql(provider.GetRequiredService<PostgresStore>().Connection)
            .UseSnakeCaseNamingConvention());        builder.Services.AddScoped<IUnitOfWork, PostgresUnitOfWork>();

        // EF Core (Npgsql): пользователи, справочники каталога, корзины, адреса, промокоды, токены.
        builder.Services.AddScoped<Domain.Users.IUserRepository, EfUserRepository>();
        builder.Services.AddScoped<Domain.Users.IAddressRepository, EfAddressRepository>();
        builder.Services.AddScoped<Domain.Catalog.ICatalogRepository, EfCatalogRepository>();
        builder.Services.AddScoped<Domain.Cart.ICartRepository, EfCartRepository>();
        builder.Services.AddScoped<Domain.Promos.IPromoRepository, EfPromoRepository>();
        builder.Services.AddScoped<Domain.Security.ISecurityTokenRepository, EfSecurityTokenRepository>();
        // Заказы — «сырой» SQL через NpgsqlCommand/NpgsqlDataReader (без EF Core).
        builder.Services.AddScoped<Domain.Orders.IOrderRepository, RawSqlOrderRepository>();

        builder.Services.AddScoped<Seeder>();
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<CatalogService>();
        builder.Services.AddScoped<CartService>();
        builder.Services.AddScoped<OrderService>();
        builder.Services.AddScoped<ProfileService>();
        builder.Services.AddScoped<AdminService>();

        // ----- Аутентификация: JWT в httpOnly-cookie murket_at -----
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "murket",
                    ValidateAudience = true,
                    ValidAudience = "murket-web",
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.JwtKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies[AuthCookies.Access];
                        return Task.CompletedTask;
                    },
                    // 401/403 от авторизации — в едином конверте {message, errors?}.
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await ErrorEnvelope.WriteAsync(context.Response, StatusCodes.Status401Unauthorized,
                            "Требуется вход в аккаунт");
                    },
                    OnForbidden = async context =>
                    {
                        await ErrorEnvelope.WriteAsync(context.Response, StatusCodes.Status403Forbidden,
                            "Доступ запрещён");
                    },
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("user", policy => policy.RequireAuthenticatedUser());
            options.AddPolicy("customer", policy => policy.RequireRole("customer"));
            options.AddPolicy("admin", policy => policy.RequireRole("admin"));
        });

        builder.Services.AddControllers();
        // Ошибки привязки модели (некорректный JSON, неверные типы полей) — в едином конверте ошибок.
        builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(pair => pair.Value is { Errors.Count: > 0 })
                    .ToDictionary(
                        pair => pair.Key.StartsWith("$.", StringComparison.Ordinal) ? pair.Key[2..] : pair.Key,
                        pair => pair.Value!.Errors.Select(error => error.ErrorMessage).ToArray());
                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
                    new { message = "Данные заполнены неверно", errors });
            };
        });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title = "Муркет API",
                Version = "v1",
                Description = "REST API интернет-магазина «Муркет» (префикс /api/v1). Swagger доступен только в Development.",
            });
            options.CustomSchemaIds(type => type.FullName!
                .Replace("MurketApi.Application.Dtos.", string.Empty)
                .Replace("Dto", string.Empty)
                .Replace("MurketApi.Domain.Common.", string.Empty));
        });

        var app = builder.Build();

        // Проверка доступа к БД до обслуживания запросов: понятная ошибка вместо стектрейса
        // посреди первого запроса (схема создаётся скриптами src/api/db — см. README).
        await using (var probe = await app.Services.GetRequiredService<NpgsqlDataSource>().OpenConnectionAsync())
        {
            await using var command = new NpgsqlCommand("SELECT 1", probe);
            await command.ExecuteScalarAsync();
        }

        // Сид — асинхронно, до обслуживания запросов (идемпотентен; в Production действует SEC-001).
        using (var scope = app.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<Seeder>().RunAsync(isDevelopment);
        }

        // Единый конверт ошибок {message, errors?} для всех доменных ошибок.
        app.UseMiddleware<ErrorHandlingMiddleware>();
        // Защитные заголовки для статики и SPA: X-Content-Type-Options, X-Frame-Options.
        app.UseMiddleware<SecurityHeadersMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        if (isDevelopment)
        {
            app.UseSwagger();
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Муркет API v1"));
        }

        app.MapControllers();
        app.MapGet("/health", () => Results.Json(new { status = "ok" }));

        // SPA fallback: все не-занятые маршруты (не /api, /health, /swagger) отдают index.html —
        // deep-link и refresh собранного Angular-клиента работают.
        app.MapFallbackToFile("index.html");

        await app.RunAsync();
    }
}

/// <summary>Точка входа для WebApplicationFactory в интеграционных тестах.</summary>
public partial class Program
{
}
