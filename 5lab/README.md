# Муркет — интернет-магазин товаров для котов

Приложение интернет-магазина: Angular-клиент и бэкенд на ASP.NET Core,
работающие вместе по контракту REST API `/api/v1`. Клиент ходит в API через
`HttpClient`; аутентификация — JWT в httpOnly-cookie (access 15 мин +
refresh 7 дней). Тема лабораторной — система авторизации: роли, передача
учётных данных, защита методов и хранение паролей описаны в разделе
«Система авторизации».

Репозиторий разделён на две части: `src/client` — Angular-приложение
(клиент), `src/api` — бэкенд (ASP.NET Core, .NET 10; подробности — в
[README](src/api/README.md) каталога и в спецификации системы из 1-й
лабораторной — `shop-specification.md`, ветка `1lab`).

## Стек

Клиент:

- Angular 20+, standalone-компоненты, TypeScript strict
- SCSS, единственный брейкпоинт проекта — **768px** (граница мобильной вёрстки)
- PrimeNG 20+ с кастомным пресетом Муркета на `@primeuix/themes` (только
  светлый режим) и primeicons
- Русская локаль: `LOCALE_ID = ru`, `registerLocaleData(localeRu)`, русские
  переводы встроенных текстов PrimeNG (`core/config/primeng-ru.ts`)
- Тесты: Karma + Jasmine (ChromeHeadless; если Chrome не установлен —
  автоматически Edge, см. `karma.conf.js`)

Бэкенд (`src/api`, .NET 10):

- REST API с версионированием в пути `/api/v1`, единый конверт ошибок
  `{message, errors?}`
- Система авторизации: две роли + публичная часть, JWT (HS256) в
  httpOnly-cookie, пароли только в виде PBKDF2-хэшей, rate-limiting входа
  и восстановления доступа
- Вход — по логину, email или телефону (нормализация `+7…`); восстановление
  доступа — по email или телефону
- Хранилище: in-memory реализации интерфейсов репозиториев (как в 3-й
  лабораторной; на схему авторизации выбор хранилища не влияет — в 4-й
  лабораторной та же схема работает на PostgreSQL)
- Health-check `GET /health`; Swagger UI — только в Development
- Тесты: xUnit (`src/api/MurketApi.Tests`)

## Установка и запуск

Требуется Node.js LTS (20.19+ или 22.12+) и npm, Chrome или Edge (для
unit-тестов в ChromeHeadless), .NET SDK 10.0.

### Режим разработки (два процесса)

```bash
dotnet run --project src/api/MurketApi     # бэкенд: http://localhost:5080
npm install                                # зависимости клиента (первый запуск)
npm start                                  # клиент: http://localhost:4200
```

`ng serve` проксирует `/api` и `/health` на бэкенд
(`src/client/proxy.conf.json`). Приложение открывается по адресу
**http://localhost:4200** (гость попадает на экран входа; после входа
покупатель — в каталог, менеджер — в раздел «Управление»).

### Production-режим (один процесс)

Клиент собирается и раздаётся бэкендом из `wwwroot` (подробности — в
[README](src/api/README.md) каталога `src/api`):

```bash
npm run build
mkdir -p src/api/MurketApi/wwwroot
cp -r dist/murket/browser/. src/api/MurketApi/wwwroot/
dotnet run --project src/api/MurketApi     # всё вместе: http://localhost:5080
```

Прочие команды:

```bash
npm test           # тесты клиента (Karma + Jasmine, ChromeHeadless/Edge)
npm run build      # production-сборка (ng build --configuration production)
npm run watch      # пересборка разработки при изменениях
dotnet test src/api/MurketApi.sln    # тесты бэкенда (xUnit)
```

## Демо-учётки и данные

Данные бэкенда — in-memory: сид выполняется при старте, изменения живут до
перезапуска процесса (перезапуск = сброс к сиду). В Development «письма» и
«SMS» восстановления доступа не отправляются: код выводится в журнал
бэкенда с маркером `[DEV-EMAIL]` (email) или `[DEV-SMS]` (телефон) — запуск
`dotnet run` показывает его в консоли.

### Демо-учётки

| Логин | Пароль | Роль | Данные |
| --- | --- | --- | --- |
| `customer` | `customer123` | покупатель | Анна Смирнова: каталог, корзина, заказы, личный кабинет |
| `admin` | `admin123` | администратор | раздел «Управление»: товары, заказы, пользователи |

Логины/пароли сид-учёток настраиваются переменными `Seed__AdminLogin`,
`Seed__AdminPassword`, `Seed__CustomerLogin`, `Seed__CustomerPassword`
(полная таблица переменных — в [README](src/api/README.md) бэкенда).

Остальные данные сида: 5 категорий и 6 брендов, 41 товар, 4 промокода,
2 адреса и корзина у покупателя, 3 заказа (№1001–1003).

## Система авторизации

### Роли

Три роли: **guest** (публичная часть — каталог и экраны входа),
**customer** (покупатель) и **admin** (администратор). Роль хранится в
claim `role` JWT и проверяется на сервере политиками ASP.NET Core
(`Program.cs`): `user` — любой аутентифицированный, `customer` и `admin` —
`RequireRole`. Контроллеры помечены `[Authorize(Policy = "...")]`:
`CartController` и `MeController` — `user`, `OrdersController` —
`customer`, `AdminController` — `admin`. Публичными остались только
`[AllowAnonymous]`-методы входа/восстановления (`login`, `refresh`,
`recovery/request`, `recovery/confirm`, `reset-password`), контроллер
каталога и `GET /health` — всё прочее без валидного JWT отвечает `401`
(чужая роль — `403`), обе ошибки — в едином конверте `{message}`.

| Эндпойнты `/api/v1` | guest | customer | admin |
| --- | :-: | :-: | :-: |
| вход, refresh, восстановление, `GET /catalog/*`, `GET /health` | ✓ | ✓ | ✓ |
| `me`, `logout`, корзина, профиль, адреса | — | ✓ | ✓ |
| оформление и свои заказы (`/orders`) | — | ✓ | — |
| раздел «Управление» (`/admin/*`) | — | — | ✓ |

На клиенте те же границы держат маршрутные гварды
(`layout/guards.ts`: `authGuard` / `guestGuard` / `customerGuard` /
`adminGuard`), но это только UX — единственный источник истины сервер.

### Передача учётных данных

1. **Вход.** Логин и пароль уходят на бэк ровно один раз — `POST
   /auth/login`. Дальше пароль не передаётся вообще.
2. **Токен в каждом авторизуемом запросе.** Сервер ставит две
   httpOnly-cookie: `murket_at` (access-JWT, HS256, 15 минут) и `murket_rt`
   (refresh, 7 дней, ротация при каждом обновлении). Браузер прикрепляет
   `murket_at` к каждому запросу автоматически; JwtBearer читает cookie,
   проверяет подпись и срок, поднимает claims. Вариант с base64 (Basic)
   не используется: пароль приходилось бы слать в каждом запросе.
   Клиентский хеш пароля вместо пароля тоже не используется:
   перехваченный хеш равносилен паролю, а сервер в любом случае хэширует
   пароль заново собственной солью (PBKDF2, см. ниже).
3. **Протухание access.** `401` перехватывает
   `core/interceptors/auth-interceptor.ts`: молча `POST /auth/refresh` и
   повтор исходного запроса; при неудаче refresh — выход на экран входа.
4. **Выход.** `POST /auth/logout` отзывает refresh-токен и стирает обе
   cookie.

Cookie — `HttpOnly` (токен недоступен из JS), `SameSite=Strict`, в
Production добавляется `Secure`.

### Пароли хранятся только в хэше

PBKDF2-HMAC-SHA256, по умолчанию 210 000 итераций (`Auth__Pbkdf2Iterations`),
случайная соль на пользователя, формат
`pbkdf2-sha256$итерации$соль$хеш` — `Verify` читает параметры из самого
хэша, поэтому смена числа итераций не ломает старые пароли. Plaintext-пароль
не хранится, не логируется и не возвращается API. Коды восстановления и
reset-токены хранятся быстрым солёным SHA-256. Перебор ограничен: 5
неудачных входов за 60 с на ключ «логин|IP» → `429`; каждая попытка входа
стоит ровно одну KDF, счётчик `auth_kdf_operations_total` логируется раз
в 60 с.

### Особенности демо-режима

- Тёмная тема фиксированно отключена (`darkModeSelector: false`) — макетами
  предусмотрена только светлая схема.
- В Production переменные `Seed__AdminPassword` и `Seed__CustomerPassword`
  обязательны и не могут совпадать с демо-значениями (guard старта,
  SEC-001 — см. README бэкенда), пароли должны содержать цифры, буквы и
  специальный символ.

## Структура каталогов

`src` состоит из двух частей: `src/client` — Angular-приложение (клиент),
`src/api` — бэкенд (ASP.NET Core, .NET 10).

Внутри `src/client/app` код располагается только в четырёх ветках; иные
отклонения недопустимы. Файлы бутстрапа сознательно разложены по тем же
веткам: маршруты и конфигурация — это «конфиг» (core), корневой компонент —
«оболочка» (layout).

```
src/client/app/core/      — сервисы (HttpClient, /api/v1) и конфигурация
    config/app.config.ts   — провайдеры бутстрапа (роутер, локаль, PrimeNG)
    config/app.routes.ts   — маршруты
    config/api.ts          — базовый префикс /api/v1
    config/primeng-ru.ts   — русские переводы PrimeNG
    config/theme.ts        — пресет темы Муркета (@primeuix/themes)
    interceptors/auth-interceptor.ts — 401 → refresh → повтор запроса
src/client/app/shared/    — валидаторы, модели, компоненты и пайпы, переиспользуемые между фичами
src/client/app/layout/    — оболочка приложения (шапка, футер, shell), guards, страницы 403/404
src/client/app/features/  — фичи: auth, catalog, cart, orders, profile, admin
    (внутри: pages и components каждой фички)
src/api/                  — бэкенд: ASP.NET Core, REST API /api/v1 (см. src/api/README.md)
    MurketApi/Program.cs   — JwtBearer (cookie→токен), политики user/customer/admin
    MurketApi/Api/Controllers/AuthController.cs — login/refresh/logout/me/recovery
    MurketApi.Infrastructure/Security/Security.cs — PBKDF2 и солёный SHA-256
src/api/MurketApi.Tests/  — тесты бэкенда (xUnit)
src/client/styles.scss    — глобальные стили
src/client/styles/        — SCSS-примеси: единственный брейкпоинт 768px
```

`src/client/main.ts`, `src/client/index.html` и `src/client/styles.scss` —
стандартные точки входа Angular CLI вне `src/client/app`.
