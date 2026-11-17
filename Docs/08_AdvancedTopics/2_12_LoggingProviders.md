# 2.12. Провайдери логування (ILoggerFactory, Console Logger)

- **Глава:** 08. Додаткові теми
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/2.12.php](https://metanit.com/sharp/efcore/2.12.php)
- **Код прикладу:** [Lesson_2_12_LoggingProviders.cs](../../src/08_AdvancedTopics/Lesson_2_12_LoggingProviders.cs)

---

## Мета уроку

Навчитися підключати та налаштовувати провайдери логування в Entity Framework Core: створювати власні провайдери через `ILoggerProvider` / `ILogger`, підключати їх локально або глобально через `ILoggerFactory`, фільтрувати події за категоріями `DbLoggerCategory` та рівнями `LogLevel`, а також використовувати стандартний консольний логер `Microsoft.Extensions.Logging.Console`.

---

## Теоретичний матеріал

Хоча метод `optionsBuilder.LogTo(...)` зручний для швидкої діагностики, у реальних застосунках зазвичай використовується стандартна інфраструктура логування .NET — `Microsoft.Extensions.Logging`.

### Власний провайдер логування (`ILoggerProvider` + `ILogger`)

Щоб реалізувати власну логіку запису логів (у файл, БД, зовнішній сервіс тощо), потрібно:
1. Реалізувати інтерфейс `ILoggerProvider`:
   - `CreateLogger(string categoryName)` — повертає екземпляр `ILogger` для вказаної категорії.
   - `Dispose()` — звільняє ресурси.
2. Реалізувати інтерфейс `ILogger`:
   - `BeginScope<TState>(TState state)` — повертає `IDisposable?` для групування логів.
   - `IsEnabled(LogLevel logLevel)` — визначає, чи активний логер для поточного рівня деталізації.
   - `Log<TState>(...)` — формує та записує повідомлення за допомогою делегата `formatter(state, exception)`.

### Способи підключення

| Спосіб | Як підключається | Коли використовувати |
| :--- | :--- | :--- |
| Локально на екземпляр | `db.GetService<ILoggerFactory>().AddProvider(...)` | Для точкового діагностування окремого екземпляра `DbContext`. |
| Глобально через фабрику | `optionsBuilder.UseLoggerFactory(MyLoggerFactory)` | Стандартний підхід: одна статична фабрика на весь тип контексту. |
| Вбудований Console Logger | `builder.AddConsole()` (пакет `Microsoft.Extensions.Logging.Console`) | Коли достатньо структурованого кольорового виводу в консоль без написання власного провайдера. |

### Фільтрація логів

Щоб не отримувати зайві технічні повідомлення EF Core (ініціалізація моделі, пул з'єднань тощо), у `LoggerFactory.Create` застосовують метод `AddFilter`:
- `DbLoggerCategory.Database.Command.Name` — категорія, що відповідає за згенеровані та виконані SQL-запити.
- `LogLevel.Information` — рівень, на якому логуються SQL-команди з параметрами та часом виконання.

---

## Ключові фрагменти коду

Глобальна фабрика логерів із фільтрацією лише SQL-команд:

```csharp
public static readonly ILoggerFactory MyLoggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddFilter((category, level) =>
            category == DbLoggerCategory.Database.Command.Name
            && level == LogLevel.Information)
        .AddConsole();
});

// У OnConfiguring:
optionsBuilder.UseLoggerFactory(MyLoggerFactory);
```

Локальне додавання власного провайдера до вже створеного контексту:

```csharp
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

var loggerFactory = db.GetService<ILoggerFactory>();
loggerFactory.AddProvider(new CustomFileAndConsoleLoggerProvider(logFilePath, filterSqlOnly: false));
```

---

## Особливості роботи в SQLite

Механізм `ILoggerFactory` та провайдери логування є **повністю незалежними від провайдера БД**. У SQLite, як і в SQL Server чи PostgreSQL, категорія `DbLoggerCategory.Database.Command` виводить точний згенерований SQLite-діалект (наприклад, `INSERT INTO "Users" ...`, `RETURNING "Id"` тощо).

---

## Підсумок та підводні камені

- **Не створюйте `LoggerFactory` всередині `OnConfiguring`!** Якщо викликати `LoggerFactory.Create(...)` при кожному створенні `DbContext`, EF Core постійно створюватиме нові екземпляри внутрішніх сервісів і швидко призведе до витоку пам'яті (EF Core навіть видасть попередження `ManyServiceProvidersCreatedWarning`). Завжди зберігайте `ILoggerFactory` у `static readonly` полі або передавайте через DI.
- **Чутливі дані:** за замовчуванням значення параметрів SQL у логах приховані (`?` або `@p0` без значень). Щоб бачити значення параметрів під час налагодження, потрібно додатково викликати `.EnableSensitiveDataLogging()`.
- **Пакет для Console Logger:** метод розширення `AddConsole()` знаходиться в окремому NuGet-пакеті `Microsoft.Extensions.Logging.Console`.
