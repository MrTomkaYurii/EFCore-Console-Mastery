# 2.2. Управління схемою БД та міграції

- **Глава:** 01. Вступ до Entity Framework Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/2.2.php](https://metanit.com/sharp/efcore/2.2.php)
- **Код прикладу:** [`Lesson_2_2_Migrations.cs`](../../src/01_Introduction/Lesson_2_2_Migrations.cs)

## Мета уроку

Зрозуміти проблему еволюції схеми БД при зміні моделей, вивчити механізм міграцій EF Core, основні CLI/PMC-команди, роботу з `IDesignTimeDbContextFactory`, програмне застосування міграцій (`Database.Migrate()`) та генерацію DDL-скриптів.

## Теоретичний матеріал

### Проблема зміни моделі
Якщо після створення БД додати в клас `User` нову властивість (наприклад, `Position`), наявна таблиця в БД не матиме такого стовпця, і запити EF Core впадуть із помилкою. Варіанти вирішення:
1. **Ручне редагування БД / SQL-скрипти** — трудомістко та схильно до помилок.
2. **`EnsureDeleted()` + `EnsureCreated()`** — перестворює БД з нуля, але **знищує всі дані**.
3. **Міграції (Migrations)** — контрольований план переходу БД від старої схеми до нової зі збереженням даних.

### Основні команди міграцій

| Дія | Visual Studio (Package Manager Console) | .NET CLI (`dotnet ef`) |
| --- | --- | --- |
| Створити міграцію | `Add-Migration InitialCreate` | `dotnet ef migrations add InitialCreate` |
| Застосувати міграцію до БД | `Update-Database` | `dotnet ef database update` |
| Згенерувати SQL-скрипт | `Script-Migration` | `dotnet ef migrations script` |
| Створити виконуваний бандл | `Bundle-Migration` | `dotnet ef migrations bundle` |

> Для роботи команд потрібні пакети `Microsoft.EntityFrameworkCore.Tools` (для PMC) та `Microsoft.EntityFrameworkCore.Design` + глобальний інструмент `dotnet tool install --global dotnet-ef` (для CLI).

### Структура файлів міграції та таблиця `__EFMigrationsHistory`
- `XXXXXXXXXXXXXX_<Name>.cs` — містить методи `Up(MigrationBuilder)` (застосування змін: `CreateTable`, `AddColumn`) та `Down(MigrationBuilder)` (відкат змін).
- `<Context>ModelSnapshot.cs` — знімок поточної моделі, за яким EF Core обчислює різницю при створенні наступної міграції.
- У самій базі даних EF Core автоматично створює службову таблицю `__EFMigrationsHistory` для обліку вже застосованих міграцій.

### `IDesignTimeDbContextFactory<TContext>`
Якщо конструктор `DbContext` приймає лише `DbContextOptions<TContext>` (наприклад, конфігурація береться з DI або `appsettings.json` у `Program.cs`), інструменти міграцій під час розробки не зможуть створити екземпляр контексту. У такому разі в проєкт додають клас, що реалізує `IDesignTimeDbContextFactory<TContext>` і повертає налаштований контекст із методу `CreateDbContext(string[] args)`.

### Програмне застосування та інспекція міграцій
- `db.Database.Migrate()` / `MigrateAsync()` — застосовує всі незастосовані міграції під час старту застосунку (зручно для локальних SQLite-застосунків).
- `db.Database.GetMigrations()` — усі міграції у збірці.
- `db.Database.GetAppliedMigrations()` — уже застосовані в БД.
- `db.Database.GetPendingMigrations()` — ті, що ще очікують застосування.
- `db.Database.GenerateCreateScript()` — генерує повний SQL DDL-скрипт створення поточної схеми.

## Ключові фрагменти коду

Інспекція стану міграцій та отримання SQL-скрипта створення схеми:

```csharp
var allMigrations = db.Database.GetMigrations().ToList();
var appliedMigrations = db.Database.GetAppliedMigrations().ToList();
var pendingMigrations = db.Database.GetPendingMigrations().ToList();

string createScript = db.Database.GenerateCreateScript();
```

## Особливості роботи в SQLite

- SQLite має обмежену підтримку `ALTER TABLE` (наприклад, історично не підтримував видалення або зміну типу деяких стовпців напряму). Провайдер EF Core для SQLite автоматично обходить це в міграціях через створення тимчасової таблиці, копіювання даних, видалення старої таблиці й перейменування нової.
- При першому застосуванні міграцій SQLite-файл створюється автоматично, якщо його ще немає. Якщо ж файл БД уже був створений раніше через `EnsureCreated()`, перша міграція впаде через конфлікт існуючих таблиць.

## Підсумок та підводні камені

- **Ніколи не викликайте `Database.EnsureCreated()`**, якщо плануєте використовувати міграції (`Migrate()` або `dotnet ef database update`).
- Перед застосуванням міграцій у продакшені обов'язково переглядайте згенеровані методи `Up()`/`Down()` або генеруйте SQL-скрипт (`dotnet ef migrations script --idempotent`) для рев'ю адміністратором БД.
- Якщо в одному проєкті є кілька різних `DbContext`, у CLI-командах потрібно явно вказувати прапорець `--context <ContextName>`.
