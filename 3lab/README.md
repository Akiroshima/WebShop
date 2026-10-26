# Муркет — интернет-магазин товаров для котов

Приложение интернет-магазина: Angular-клиент и бэкенд на ASP.NET Core,
работающие вместе по контракту REST API `/api/v1`. Клиент ходит в API через
`HttpClient` (браузерный мок-слой 2-й лабораторной удалён); аутентификация —
JWT в httpOnly-cookie (access 15 мин + refresh 7 дней).

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
- Аутентификация: JWT в httpOnly-cookie, PBKDF2-хэширование паролей,
  rate-limiting входа и восстановления доступа
- Вход — по логину, email или телефону (нормализация `+7…`); восстановление
  доступа — по email или телефону
- Хранилище: in-memory реализации интерфейсов репозиториев (подготовлено к
  замене на реальную БД без изменения контроллеров)
- Health-check `GET /health`; Swagger UI — только в Development
- Тесты: xUnit — `src/api/MurketApi.Tests`

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
| `customer` | `customer123` | покупатель | Анна Смирнова: профиль с телефоном, 2 адреса, корзина из 3 позиций, 3 заказа |
| `admin` | `admin123` | администратор | раздел «Управление»: товары, заказы, пользователи |

Логины/пароли сид-учёток и включение демо-набора настраиваются переменными
`Seed__AdminLogin`, `Seed__AdminPassword`, `Seed__CustomerLogin`,
`Seed__CustomerPassword`, `Seed__DemoData` (полная таблица переменных — в
[README](src/api/README.md) бэкенда).

Остальные данные сида: 5 категорий и 6 брендов, 41 товар (2 неактивных —
демо скрытых позиций каталога), 4 промокода (`МУР10` — −10 % от 1000 ₽,
`СКИДКА500` — −500 ₽ от 3500 ₽, `ПРОМО` — истёкший, `ЛИМИТ` — исчерпавший
лимит использований), 3 заказа (№1001–1003), следующий номер заказа — 1004.

### Особенности демо-режима

- Тёмная тема фиксированно отключена (`darkModeSelector: false`) — макетами
  предусмотрена только светлая схема.
- В Production переменные `Seed__AdminPassword` и `Seed__CustomerPassword`
  обязательны и не могут совпадать с демо-значениями (guard старта,
  SEC-001 — см. README бэкенда).

## Структура каталогов

`src` состоит из двух частей: `src/client` — Angular-приложение (клиент),
`src/api` — бэкенд (ASP.NET Core, .NET 10).

Внутри `src/client/app` код располагается только в четырёх ветках; иные
отклонения недопустимы. Файлы бутстрапа сознательно разложены по тем же
веткам: маршруты и конфигурация — это «конфиг» (core), корневой компонент —
«оболочка» (layout).

```
src/client/app/core/      — сервисы (HttpClient, /api/v1), интерцептор сессии и конфигурация
    config/app.config.ts   — провайдеры бутстрапа (роутер, локаль, PrimeNG)
    config/app.routes.ts   — маршруты
    config/api.ts          — базовый префикс /api/v1
    config/primeng-ru.ts   — русские переводы PrimeNG
    config/theme.ts        — пресет темы Муркета (@primeuix/themes)
src/client/app/shared/    — валидаторы, модели, компоненты и пайпы, переиспользуемые между фичами
src/client/app/layout/    — оболочка приложения (шапка, футер, shell), guards, страницы 403/404
src/client/app/features/  — фичи: auth, catalog, cart, orders, profile, admin
    (внутри: pages и components каждой фички)
src/api/                  — бэкенд: ASP.NET Core, REST API /api/v1 (см. src/api/README.md)
src/api/MurketApi.Tests/  — тесты бэкенда (xUnit)
src/client/main.ts, src/client/index.html и src/client/styles.scss —
                          стандартные точки входа Angular CLI вне src/client/app
```
