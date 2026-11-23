# Шпаргалка лектора: Глава 1. Вступ до Entity Framework Core

> **Швидкий старт із кореня репозиторію:**
> - Інтерактивне меню (обрати главу `1`): `dotnet run --project src/EFCoreMastery.ConsoleApp`
> - Запуск конкретного уроку: `dotnet run --project src/EFCoreMastery.ConsoleApp -- <ID>`
> - Перед заняттям перевірте збірку: `dotnet build EFCore-Console-Mastery.slnx`

## План проведення та запуски

| ID | Тема та конспект | Про що говорити (ключові тези) | Команда запуску та CLI | На що звернути увагу у виводі |
|---|---|---|---|---|
| **1.1** | [Що таке EF Core](1_01_WhatIsEfCore.md) · [код](../../src/01_Introduction/Lesson_1_01_WhatIsEfCore.cs) | • Що таке ORM і навіщо EF Core (робота з C#-класами замість ручного SQL/ADO.NET).<br>• Кросплатформність, архітектура провайдерів БД (`Sqlite`, `SqlServer`, `Npgsql`, `Pomelo`).<br>• Як EF Core будує внутрішню модель метаданих `IModel` на основі `DbContext`. | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 1.1` | • Ім'я активного провайдера (`Microsoft.EntityFrameworkCore.Sqlite`).<br>• Інспекція `IModel`: які таблиці й стовпці EF Core вивів автоматично.<br>• Результат базової LINQ-вибірки. |
| **1.2** | [Перший застосунок на EF Core](1_02_FirstApp.md) · [код](../../src/01_Introduction/Lesson_1_02_FirstApp.cs) | • 3 кити першого застосунку: модель (`User`), контекст (`DbContext` + `DbSet<User>`), метод `OnConfiguring`.<br>• Призначення `Database.EnsureCreated()` для швидкого прототипування.<br>• Чому після `Add()` дані ще не в БД, доки не викликано `SaveChanges()`. | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 1.2` | • Автоматичне заповнення `Id` після `SaveChanges()`.<br>• Повторне читання тих самих об'єктів із нового екземпляра `DbContext`. |
| **1.3** | [Підключення до існуючої БД](1_03_ExistingDatabase.md) · [код](../../src/01_Introduction/Lesson_1_03_ExistingDatabase.cs) | • Підхід **Database First** vs **Code First**.<br>• Реверс-інжиніринг існуючої БД (Scaffolding) через `dotnet ef dbcontext scaffold` або `Scaffold-DbContext`.<br>• Чому не можна редагувати згенеровані класи напряму без `partial` (перезапишуться при `--force`). | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 1.3`<br><br>*(Додатково показати CLI нижче)* | • Спочатку БД створюється «сирим» SQL без EF Core.<br>• Потім `DbContext` підключається до готового файлу `.db` і читає рядки без `EnsureCreated()`. |
| **2.16** | [Управління базою даних](2_16_DatabaseManagement.md) · [код](../../src/01_Introduction/Lesson_2_16_DatabaseManagement.cs) | • Методи `DatabaseFacade`: `EnsureDeleted()`, `EnsureCreated()`, `CanConnect()` та їхні `Async`-версії.<br>• Повертане значення `bool` (чи була БД реально створена/видалена).<br>• Чому `EnsureCreated()` несумісний із міграціями. | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 2.16` | • `EnsureCreated()` повертає `true` при першому виклику і `false`, якщо БД уже існує.<br>• `CanConnect()` повертає `true` для доступного файлу SQLite. |
| **1.4** | [Основні операції з даними. CRUD](1_04_CrudOperations.md) · [код](../../src/01_Introduction/Lesson_1_04_CrudOperations.cs) | • Повний цикл CRUD (`Add`/`AddRange`, `ToList`, `Update`/`UpdateRange`, `Remove`/`RemoveRange`).<br>• Життєвий цикл `EntityState` (`Detached`, `Added`, `Unchanged`, `Modified`, `Deleted`).<br>• **Ключова різниця:** Connected (відстежуване) vs Disconnected (від'єднане) оновлення.<br>• Коли потрібен `SaveChangesAsync()` і чому `AddAsync()` потрібен рідко. | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 1.4` | • У Connected-сценарії `Update()` не викликається — `ChangeTracker` сам бачить зміни.<br>• У Disconnected-сценарії без явного `db2.Users.Update(user)` зміни не збережуться.<br>• Зміна списку після кожного кроку CRUD. |
| **1.5** | [Конфігурація підключення](1_05_ConnectionConfiguration.md) · [код](../../src/01_Introduction/Lesson_1_05_ConnectionConfiguration.cs) | • 3 способи передати налаштування: `OnConfiguring`, конструктор із `DbContextOptions<T>`, зовнішній `appsettings.json` через `ConfigurationBuilder`.<br>• Чому не можна хардкодити рядки підключення в коді (безпека, різні середовища Dev/Prod). | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 1.5` | • Один і той самий контекст працює з різними джерелами конфігурації.<br>• Коректне зчитування `ConnectionStrings` із JSON. |
| **1.6** | [Логування операцій](1_06_Logging.md) · [код](../../src/01_Introduction/Lesson_1_06_Logging.cs) | • Вбудований метод `optionsBuilder.LogTo(...)` (вивід у консоль, `Debug`, файл).<br>• Мінімальний рівень `LogLevel.Information` vs `Warning`.<br>• Фільтрація шуму: за подією `RelationalEventId.CommandExecuted` та за категорією `DbLoggerCategory.Database.Command`. | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 1.6` | • Порівняти обсяг логу без фільтра і з фільтром `CommandExecuted`.<br>• Показати аудиторії реальний параметризований SQL (`@p0`, `@p1`), який генерує EF Core. |
| **2.2** | [Управління схемою БД та міграції](2_02_Migrations.md) · [код](../../src/01_Introduction/Lesson_2_02_Migrations.cs) | • Навіщо міграції в продакшені (еволюція схеми без втрати даних).<br>• 3 файли міграції (`Up`/`Down`, `.Designer.cs`, `ModelSnapshot.cs`) та службова таблиця `__EFMigrationsHistory`.<br>• Роль `IDesignTimeDbContextFactory<T>` для CLI.<br>• Деплой: `script` vs `bundle` vs `Database.Migrate()`. | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 2.2`<br><br>*(Обов'язково показати CLI нижче)* | • Інспекція `GetMigrations()` / `GetAppliedMigrations()`.<br>• Згенерований DDL-скрипт через `Database.GenerateCreateScript()`. |

---

## Довідник CLI-команд для демонстрації в Главі 1

### 1. Встановлення інструментів (показати перед уроками `1.3` та `2.2`)
```powershell
# Встановити глобальний CLI-інструмент dotnet-ef (один раз на машину)
dotnet tool install --global dotnet-ef

# Оновити до актуальної версії (якщо вже встановлено)
dotnet tool update --global dotnet-ef

# Перевірити встановлену версію
dotnet ef --version
```

### 2. Скафолдинг існуючої БД (до уроку `1.3`)
```powershell
# Потрібні пакети у цільовому проєкті:
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design

# .NET CLI: згенерувати DbContext та моделі з існуючого файлу SQLite
dotnet ef dbcontext scaffold "Data Source=helloapp.db" Microsoft.EntityFrameworkCore.Sqlite -o Models -c HelloAppContext

# Те саме у Visual Studio (Package Manager Console):
# Scaffold-DbContext "Data Source=helloapp.db" Microsoft.EntityFrameworkCore.Sqlite -OutputDir Models
```
> **Пояснення для аудиторії:** прапорець `-o` (`--output-dir`) задає папку для моделей, `-c` (`--context`) — ім'я контексту, `--force` перезаписує існуючі файли, `-t TableName` дозволяє згенерувати лише обрані таблиці.

### 3. Повний цикл міграцій (до уроку `2.2`)

| Дія | .NET CLI (`dotnet ef`) | Visual Studio (PMC) | Коли пояснювати |
|---|---|---|---|
| Створити міграцію | `dotnet ef migrations add InitialCreate` | `Add-Migration InitialCreate` | Після зміни класів моделей або `OnModelCreating` |
| Застосувати до БД | `dotnet ef database update` | `Update-Database` | Створює/оновлює таблиці та записує рядок у `__EFMigrationsHistory` |
| Відкотити до версії | `dotnet ef database update InitialCreate` | `Update-Database InitialCreate` | Викликає метод `Down()` для новіших міграцій |
| Видалити останню (незастосовану) | `dotnet ef migrations remove` | `Remove-Migration` | Тільки поки міграція не застосована в БД (або після відкату) |
| Згенерувати SQL-скрипт | `dotnet ef migrations script --idempotent -o migrate.sql` | `Script-Migration -Idempotent` | Рекомендований спосіб для DBA / CI-CD у продакшені |
| Зібрати виконуваний бандл | `dotnet ef migrations bundle` | `Bundle-Migration` | Самодостатній `.exe` для деплою міграцій без SDK на сервері |

> **Порада лектору:** якщо у проєкті кілька `DbContext`, завжди додавайте `--context <ContextName>` (наприклад, `dotnet ef migrations add Init --context ApplicationContext`).
