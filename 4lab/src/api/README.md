# API — бэкенд «Муркета»

Серверная часть системы: монолитное приложение на **ASP.NET Core** (.NET 10),
которое обслуживает REST API `/api/v1` и раздаёт статику собранного
Angular-клиента (`src/client`). Хранилище — **PostgreSQL 16**: EF Core
(Npgsql) для пользователей, справочников каталога, корзин, адресов,
промокодов и токенов безопасности; репозиторий заказов — на «сыром»
ADO.NET (`NpgsqlCommand`/`NpgsqlDataReader`); единица работы — транзакция
БД (см. раздел 5). Схема создаётся SQL-скриптами [`db/`](db/README.md).
Подробности — спецификация системы из 1-й лабораторной
(`shop-specification.md`, ветка `1lab`).

Ключевые положения:

- REST API с версионированием в пути: `/api/v1/...`; единый конверт ошибок
  `{message, errors?}`
- Аутентификация: JWT в httpOnly-cookie (access 15 мин + refresh 7 дней);
  CORS не настраивается никогда
- Вход — по логину, email или телефону (нормализация `+7…`); восстановление
  доступа — по email или телефону, без оракула существования контакта
- Health-check `GET /health` → `{"status":"ok"}`; Swagger UI только в
  Development
- SPA fallback: все не-`/api` маршруты отдают `index.html` (с
  `X-Content-Type-Options: nosniff` и `X-Frame-Options: DENY`)
- Конфигурация — только переменные окружения (файлы `appsettings*.json` не
  используются)

---

## 1. Требования и запуск

Требуются **.NET SDK 10.0** и **PostgreSQL 16**. Один раз подготовьте
базу (создание БД и роли, схема 3НФ, демо-данные — см.
[`db/README.md`](db/README.md)):

```bash
psql -h 127.0.0.1 -p 5432 -U postgres -d postgres -f db/01_database.sql
psql -h 127.0.0.1 -p 5432 -U murket   -d murket   -f db/02_schema.sql
psql -h 127.0.0.1 -p 5432 -U murket   -d murket   -f db/03_seed_demo.sql
```

```bash
dotnet run --project src/api/MurketApi
```

Бэкенд поднимается на **http://localhost:5080** (адрес задан в
`src/api/MurketApi/Properties/launchSettings.json`; переопределяется
переменной `ASPNETCORE_URLS`). При `dotnet run` без явного профиля
приложение стартует в окружении Development; для прод-режима задайте
`ASPNETCORE_ENVIRONMENT`. Проверка живости: `GET /health`; Swagger UI —
`http://localhost:5080/swagger` (только в Development, вне Development
`/swagger*` отвечает 404).

Переменные окружения и значения по умолчанию:

| Переменная | По умолчанию | Описание |
| --- | --- | --- |
| `Auth__JwtKey` | не задана: в Development используется эпизодический случайный ключ (перегенерируется при каждом старте, в журнал пишется warning; токены не переживают перезапуск) | Ключ подписи JWT (HS256). В Production обязательна и должна содержать не менее 32 символов |
| `Auth__AccessTtlMinutes` | `15` | TTL access-cookie в минутах (целое > 0) |
| `Auth__RefreshTtlDays` | `7` | TTL refresh-cookie в днях (целое > 0) |
| `Auth__Pbkdf2Iterations` | `210000` | Число итераций PBKDF2-HMAC-SHA256 для новых хэшей (целое от 1 до 10 000 000; `Verify` читает параметры из самого хэша) |
| `Auth__TokenHashSalt` | не задана: используется `Auth__JwtKey` | «Перец» SHA-256-хэша refresh/reset-токенов и кодов восстановления. По умолчанию выводится из ключа JWT, поэтому хэши переживают перезапуск процесса и refresh-токены из PostgreSQL остаются действительными |
| `Database__ConnectionString` | `Host=127.0.0.1;Port=5432;Database=murket;Username=murket;Password=murket` (только Development) | Строка подключения к PostgreSQL. В Production обязательна (см. `db/README.md`) |
| `Seed__AdminLogin` | `admin` | Логин сид-учётки администратора |
| `Seed__AdminPassword` | `admin123` (только Development; см. предупреждение в разделе 3) | Пароль сид-учётки администратора |
| `Seed__CustomerLogin` | `customer` | Логин сид-учётки покупателя |
| `Seed__CustomerPassword` | `customer123` (только Development; см. предупреждение в разделе 3) | Пароль сид-учётки покупателя |
| `Seed__DemoData` | умолчание по окружению: Development — `true`, вне Development — всегда `false` | Демо-набор данных; допустимые явные значения — `true` / `false` (некорректное значение даёт warning и умолчание окружения) |

Нарушения правил валидации (например, `Auth__JwtKey` короче 32 символов в
Production или `Auth__Pbkdf2Iterations` вне диапазона) останавливают старт
приложения с сообщением, называющим переменную окружения. Старт также
останавливается с понятной ошибкой, если PostgreSQL недоступен по
`Database__ConnectionString` (проверка `SELECT 1` до сида и обслуживания
запросов).

## 2. Схема dev-режима и сборка клиента для прод

**Development** — два процесса:

1. Бэкенд: `dotnet run --project src/api/MurketApi` → Kestrel на
   http://localhost:5080.
2. Клиент: из корня 4-й лабораторной `npm install && npm start`
   (`ng serve`) → Angular dev-server на http://localhost:4200. Прокси
   описан в `src/client/proxy.conf.json`: пути `/api` и `/health` прозрачно
   проксируются на `http://localhost:5080` (Kestrel), поэтому клиент
   обращается только к собственному origin — CORS не требуется и не
   настраивается.

**Production** — клиент собирается и раздаётся бэкендом из `wwwroot`:

```bash
npm run build                       # ng build --configuration production → dist/murket/browser
cp -r dist/murket/browser/. src/api/MurketApi/wwwroot/
dotnet run --project src/api/MurketApi
```

Копирование сборки в `wwwroot` выполняется вручную (отдельная MSBuild-цель
не предусмотрена). Статику отдаёт Kestrel с защитными заголовками
(`nosniff`, `DENY`); неизвестные не-`/api` маршруты отдают `index.html`
(SPA fallback).

Сборка и проверки (локальный эквивалент CI):

```bash
dotnet build src/api/MurketApi.sln                    # 0 предупреждений (TreatWarningsAsErrors)
dotnet test src/api/MurketApi.sln                     # юнит-тесты бэкенда (xUnit, реальная PostgreSQL)
dotnet test tests/integration/MurketIntegration.slnx  # интеграционные зоны B-01…B-23
npm run build                                         # из корня 4-й лабораторной (клиент)
npm test                                              # из корня 4-й лабораторной, ChromeHeadless (fallback — Edge)
```

Тесты работают с реальной PostgreSQL (роль `murket` с правом CREATEDB):
юнит-тесты (`src/api/MurketApi.Tests`) создают каждому тесту собственную
базу мгновенным клоном шаблона; интеграционные зоны
(`tests/integration/B-01…B-23`, по одной на батч кейсов) проверяют
HTTP-контракты `/api/v1` через `WebApplicationFactory` на изолированной
базе зоны, а зона **B-23** дополнительно проверяет целостность БД напрямую
SQL (транзакционность оформления, снимки заказов, каскады внешних ключей,
уникальность номеров). Строка подключения тестов переопределяется
переменной `MurketTests__ConnectionString` (умолчание —
`Host=127.0.0.1;Port=5432;Database=postgres;Username=murket;Password=murket`).

## 3. Сид-учётки и предупреждение SEC-001

Сид выполняется при старте, до обслуживания запросов, и идемпотентен
(при наличии пользователей пропускается; повторный запуск не дублирует и не
изменяет существующие записи). Тот же демо-набор воспроизводится чистым SQL
— скриптом `db/03_seed_demo.sql` (идентификаторы и данные совпадают).

**Development:**

| Логин | Пароль | Роль | Примечание |
| --- | --- | --- | --- |
| `admin` (или `Seed__AdminLogin`) | `admin123` (или `Seed__AdminPassword`) | администратор | создаётся всегда |
| `customer` (или `Seed__CustomerLogin`) | `customer123` (или `Seed__CustomerPassword`) | покупатель | создаётся всегда |

Демо-набор (умолчание в Development; `Seed__DemoData=false` отключает):
5 категорий, 6 брендов, 41 товар (2 неактивных), 2 адреса и корзина из 3
позиций у покупателя, 4 промокода (`МУР10`, `СКИДКА500`, `ПРОМО` — истёкший,
`ЛИМИТ` — исчерпавший лимит), 3 заказа (№1001–1003), следующий номер
заказа — 1004.

> **ВНИМАНИЕ (SEC-001).** В окружении **Production** старт приложения
> **блокируется** (fail-fast до сида и обслуживания запросов), если
> `Seed__AdminPassword` или `Seed__CustomerPassword` не задана, пуста или
> равна значению по умолчанию. Для прод-развёртывания требуются
> **собственные пароли**, удовлетворяющие правилам пароля системы: длина
> 8–128 символов, обязательно цифры, буквы и специальный символ (пароль не
> тримуется). Значения по умолчанию для прод-развёртываний намеренно не
> документируются — не используйте значения из этого README в Production.
> Дополнительно в Production обязательна `Auth__JwtKey` (не менее 32
> символов), `Database__ConnectionString`, а cookie получают флаг `Secure`.

## 4. Оперативная заметка: KDF-поверхность и алертинг

Каждая операция вывода ключа (PBKDF2-HMAC-SHA256,
`Auth__Pbkdf2Iterations`, по умолчанию 210 000 итераций) инкрементирует
метрику `auth_kdf_operations_total{caller}` (caller ∈ `login`,
`change_password`, `reset_password`, `seed`, `reference`); суммарное
значение логируется раз в 60 секунд. Реестр остаточных KDF-поверхностей:

- **вход** (`POST /auth/login`): ровно одна KDF на каждую попытку входа;
  сдерживание — 5 неудачных попыток за 60-секундное окно на ключ
  «логин|IP» (ротация логинов с одного IP сохраняет поверхность
  распределённых неудачных входов — пер-IP сдерживание в этой итерации
  заморожено, поверхность ПРИНЯТА осознанно);
- **восстановление доступа** (`recovery/request`, `recovery/confirm`):
  0 операций KDF (коды восстановления и reset-токены хэшируются быстрым
  солёным SHA-256 с перцем `Auth__TokenHashSalt`), амплитуда CPU пренебрежима;
- **смена/сброс пароля** (`me/password`, `reset-password`): KDF только
  после предъявления валидной сессии/токена — неаутентифицированная
  амплитуда 0.

Регистрации в системе нет; иные (неаутентифицированные) KDF-поверхности в
системе запрещены.

**Алертинг:** порог `auth_kdf_operations_total` **> 100/с в течение 5
минут** — признак перебора; при штатной работе интенсивность на порядки
ниже.

Частотные границы: неудачные входы — 5/60 с на ключ «логин|IP» (проверка
выполняется только на ветке неудачного пароля, после ровно одной KDF);
запросы кода восстановления — 3/3600 с на контакт (email или телефон);
подтверждение кода — не более 5 неверных попыток, после чего код
сгорает. Превышение → 429 с телом
`{"message":"Слишком много попыток. Повторите позже"}`.

## 5. Хранилище: PostgreSQL через EF Core + сырой SQL

Данные живут в **PostgreSQL 16** (схема 3НФ — [`db/02_schema.sql`](db/02_schema.sql),
полное описание — в [`db/README.md`](db/README.md)). Один HTTP-запрос —
одно соединение scope-а (`PostgresStore`), которым совместно пользуются
обе технологии; `IUnitOfWork` (`PostgresUnitOfWork`) открывает на нём
транзакцию, поэтому многорепозитарные операции атомарны.

- **EF Core (Npgsql)** — `EfUserRepository`, `EfCatalogRepository`,
  `EfCartRepository`, `EfAddressRepository`, `EfPromoRepository`,
  `EfSecurityTokenRepository`: доменные сущности отображаются в схему
  напрямую (private-сеттеры EF устанавливает через backing fields, позиции
  корзины — owned-коллекция `cart_items`, строковые идентификаторы
  конвертируются в `uuid`); изменения отслеживаемых сущностей фиксируются
  `SaveChanges` в конце единицы работы.
- **Сырой ADO.NET** — `RawSqlOrderRepository`: агрегат «заказ + позиции»
  читается и пишется явными запросами (`NpgsqlCommand`/`NpgsqlDataReader`),
  номера заказов выдаёт sequence `order_number_seq`.
- Оформление заказа атомарно: вставка заказа (сырой SQL) + списание
  остатков и рост `used_count` промокода (EF Core) + очистка корзины
  commit-ятся одной транзакцией; отмена заказа возвращает остатки тем же
  способом.

## 6. Dev-почта и dev-SMS (восстановление доступа)

- **Development:** письма и «SMS» восстановления отправляются только в
  журнал — категория `EmailDev` с маркером `[DEV-EMAIL]` и полным текстом
  письма (адресат, тема, код) и категория `SmsDev` с маркером `[DEV-SMS]`
  (номер и код). Это **единственное место**, где код восстановления
  появляется.
- **Остальные среды:** письмо/SMS не отправляется и не логируется (заглушка
  пишет один warning без адресата и содержимого); код восстановления вне
  категорий `EmailDev`/`SmsDev` не появляется никогда.

Эндпойнт `POST /auth/recovery/request` отвечает `200` с
`{channel, maskedContact}` независимо от существования контакта (оракула
нет) — см. журнал `EmailDev`/`SmsDev`, чтобы увидеть код в Development.
