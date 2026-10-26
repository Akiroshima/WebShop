using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var isDevelopment = builder.Environment.IsDevelopment();

        // Конфигурация — только переменные окружения (файлы appsettings*.json не используются;
        // источники по умолчанию, включая json-файлы, отключены).
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddEnvironmentVariables();

        var auth = AuthOptions.FromEnvironment(builder.Configuration, isDevelopment, builder.Environment);
        var seed = SeedOptionsReader.FromEnvironment(builder.Configuration, isDevelopment, builder.Environment);

        // ----- DI: хранилище-заглушка, безопасность, сервисы -----
        builder.Services.AddSingleton(auth);
        builder.Services.AddSingleton(seed);
        builder.Services.AddSingleton<InMemoryDb>();
        builder.Services.AddSingleton<IUnitOfWork, InMemoryUnitOfWork>();
        builder.Services.AddSingleton<Domain.Common.IClock, UtcClock>();
        builder.Services.AddSingleton<ISecretGenerator, CryptoSecretGenerator>();
        builder.Services.AddSingleton<ITokenHasher, SaltedSha256TokenHasher>();
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

        builder.Services.AddSingleton<Seeder>();

        // Репозитории — заглушки БД (in-memory); контроллеры и прикладные сервисы зависят
        // только от интерфейсов Domain, поэтому замена на EF Core не затронет логику.
        builder.Services.AddSingleton<Domain.Users.IUserRepository, InMemoryUserRepository>();
        builder.Services.AddSingleton<Domain.Users.IAddressRepository, InMemoryAddressRepository>();
        builder.Services.AddSingleton<Domain.Catalog.ICatalogRepository, InMemoryCatalogRepository>();
        builder.Services.AddSingleton<Domain.Cart.ICartRepository, InMemoryCartRepository>();
        builder.Services.AddSingleton<Domain.Orders.IOrderRepository, InMemoryOrderRepository>();
        builder.Services.AddSingleton<Domain.Promos.IPromoRepository, InMemoryPromoRepository>();
        builder.Services.AddSingleton<Domain.Security.ISecurityTokenRepository, InMemorySecurityTokenRepository>();

        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<CatalogService>();
        builder.Services.AddSingleton<CartService>();
        builder.Services.AddSingleton<OrderService>();
        builder.Services.AddSingleton<ProfileService>();
        builder.Services.AddSingleton<AdminService>();

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

        // Сид — синхронно, до обслуживания запросов (идемпотентен; в Production действует SEC-001).
        app.Services.GetRequiredService<Seeder>().Run(isDevelopment);

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

        app.Run();
    }
}

/// <summary>Точка входа для WebApplicationFactory в интеграционных тестах.</summary>
public partial class Program
{
}
