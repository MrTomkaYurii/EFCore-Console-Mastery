# 1.1. Що таке Entity Framework Core

- **Глава:** 01. Вступ до Entity Framework Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/1.1.php](https://metanit.com/sharp/efcore/1.1.php)
- **Код прикладу:** [`Lesson_1_1_WhatIsEfCore.cs`](../../src/01_Introduction/Lesson_1_1_WhatIsEfCore.cs)

## Мета уроку

Зрозуміти призначення Entity Framework Core як ORM-технології, ознайомитися з базовими концепціями (сутність, ключ, зв'язки, LINQ-трансляція), структурою NuGet-пакетів EF Core та навчитися інспектувати метадані моделі (`IModel`) у SQLite.

## Теоретичний матеріал

### ORM-підхід та кросплатформність
**Entity Framework Core (EF Core)** — це об'єктно-реляційний мапер (**ORM** — *Object-Relational Mapping*) від Microsoft для платформи .NET. Він дозволяє працювати з реляційною базою даних не на рівні таблиць, рядків і сирого SQL, а на рівні звичайних C#-класів та їхніх колекцій.

- Працює поверх .NET (консольні застосунки, ASP.NET Core, WPF, WinForms, MAUI тощо) на Windows, Linux і macOS.
- Має відкритий вихідний код: [github.com/dotnet/efcore](https://github.com/dotnet/efcore).
- Надає універсальний API: при зміні СУБД (наприклад, із SQLite на PostgreSQL чи SQL Server) основний код запитів і збереження даних залишається незмінним — змінюється переважно лише конфігурація провайдера.

### Ключові концепції
- **Сутність (Entity):** C#-клас, об'єкти якого відображаються на записи в таблиці БД.
- **Властивості (Properties):** зіставляються зі стовпцями таблиці (як прості типи `int`, `string`, `decimal`, так і комплексні типи).
- **Первинний ключ (Primary Key):** властивість (або набір властивостей), що унікально ідентифікує кожен екземпляр сутності.
- **Асоціативні зв'язки:** `1:1`, `1:N` та `M:N` між сутностями, що відповідають зовнішнім ключам у реляційній БД.
- **LINQ-запити:** EF Core транслює вирази LINQ (`.Where()`, `.OrderBy()`, `.Select()`) у SQL-запити конкретної СУБД.

### Основні NuGet-пакети EF Core

| Пакет | Призначення |
| --- | --- |
| `Microsoft.EntityFrameworkCore` | Базове ядро EF Core (зазвичай підтягується транзитивно провайдером) |
| `Microsoft.EntityFrameworkCore.Sqlite` | Провайдер для SQLite включно з нативними бінарниками рушія |
| `Microsoft.EntityFrameworkCore.Sqlite.Core` | Провайдер SQLite без нативних бінарних файлів |
| `Microsoft.EntityFrameworkCore.SqlServer` | Провайдер для Microsoft SQL Server та Azure SQL |
| `Microsoft.EntityFrameworkCore.Cosmos` | Провайдер для Azure Cosmos DB |
| `Microsoft.EntityFrameworkCore.InMemory` | Провайдер БД у пам'яті (для базового тестування) |
| `Microsoft.EntityFrameworkCore.Tools` | Команди PowerShell для Package Manager Console (`Add-Migration`, `Scaffold-DbContext`) |
| `Microsoft.EntityFrameworkCore.Design` | Компоненти часу розробки (міграції, реверс-інжиніринг через `dotnet ef`) |
| `Microsoft.EntityFrameworkCore.Proxies` | Підтримка Lazy Loading та проксі відстеження змін |
| `Microsoft.EntityFrameworkCore.Relational` | Спільні компоненти для всіх реляційних провайдерів |
| `Microsoft.EntityFrameworkCore.Abstractions` | Абстракції EF Core, незалежні від СУБД |
| `Microsoft.EntityFrameworkCore.Analyzers` | Roslyn-аналізатори коду C# для EF Core |

## Ключові фрагменти коду

Інспекція внутрішньої моделі EF Core (`db.Model`) показує, як C#-властивості перетворилися на стовпці SQLite:

```csharp
foreach (var entityType in db.Model.GetEntityTypes())
{
    Console.WriteLine($"Сутність: {entityType.ClrType.Name} -> Таблиця: {entityType.GetTableName()}");
    foreach (var prop in entityType.GetProperties())
    {
        Console.WriteLine($"  {prop.Name} ({prop.ClrType.Name}) -> {prop.GetColumnName()} [{prop.GetColumnType()}]");
    }
}
```

Вибірка даних через LINQ без написання SQL вручну:

```csharp
var expensiveProducts = db.Products
    .Where(p => p.Price > 2000m)
    .OrderByDescending(p => p.Price)
    .ToList();
```

## Особливості роботи в SQLite

- У SQLite нативна система типів динамічна й обмежена (`INTEGER`, `TEXT`, `REAL`, `BLOB`). Наприклад, `int` зіставляється з `INTEGER`, `string` — з `TEXT`, а `decimal` у провайдері SQLite за замовчуванням зберігається як `TEXT` (оскільки SQLite не має точного десяткового типу `DECIMAL`, а `REAL` є числом з плаваючою комою).
- Пакет `Microsoft.EntityFrameworkCore.Sqlite` уже включає `Microsoft.EntityFrameworkCore` та `Microsoft.EntityFrameworkCore.Relational`, тому в навчальному проєкті достатньо посилання на нього.

## Підсумок та підводні камені

- EF Core — це абстракція над БД, але розуміння згенерованого SQL залишається критично важливим для продуктивності.
- Для типу `decimal` у SQLite операції порівняння/сортування на стороні БД можуть мати обмеження (у новіших версіях EF Core частина з них обмежена або потребує конвертерів у `double`/`long`).
- Не використовуйте `Microsoft.EntityFrameworkCore.InMemory` для перевірки реляційних обмежень (зовнішніх ключів, транзакцій) — для цього краще підходить SQLite.
