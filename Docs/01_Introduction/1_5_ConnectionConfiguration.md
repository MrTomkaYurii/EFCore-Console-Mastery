# 1.5. Конфігурація підключення

- **Глава:** 01. Вступ до Entity Framework Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/1.5.php](https://metanit.com/sharp/efcore/1.5.php)
- **Код прикладу:** [`Lesson_1_5_ConnectionConfiguration.cs`](../../src/01_Introduction/Lesson_1_5_ConnectionConfiguration.cs)

## Мета уроку

Вивчити способи налаштування підключення до бази даних у `DbContext`: перевизначення `OnConfiguring`, передачу `DbContextOptions<TContext>` через конструктор та зчитування рядка підключення із зовнішньої JSON-конфігурації за допомогою `ConfigurationBuilder`.

## Теоретичний матеріал

### Два основні підходи до конфігурації `DbContext`

| Підхід | Як реалізується | Коли доречно використовувати |
| --- | --- | --- |
| **`OnConfiguring(DbContextOptionsBuilder)`** | Перевизначення методу всередині класу контексту | Прості консольні утиліти, швидкі прототипи або передача рядка підключення в конструктор |
| **`DbContextOptions<TContext>` у конструкторі** | Передача налаштувань ззовні: `base(options)` | Промислові застосунки, ASP.NET Core з Dependency Injection, модульні тести |

### Зовнішня конфігурація (`appsettings.json`)
Зберігати рядок підключення безпосередньо в коді C# небезпечно й незручно (зміна БД вимагатиме перекомпіляції). Зазвичай рядок підключення виносять у JSON-файл `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=helloapp.db"
  }
}
```

Для роботи з JSON-конфігурацією використовується пакет `Microsoft.Extensions.Configuration.Json`:
- `ConfigurationBuilder` збирає джерела налаштувань (`SetBasePath`, `AddJsonFile("appsettings.json")` або `AddJsonStream(...)`).
- Метод розширення `config.GetConnectionString("DefaultConnection")` зчитує значення з секції `"ConnectionStrings"`.

## Ключові фрагменти коду

Передача `DbContextOptions<TContext>` у контекст:

```csharp
public class OptionsContext(DbContextOptions<OptionsContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
}

var options = new DbContextOptionsBuilder<OptionsContext>()
    .UseSqlite(connectionString)
    .Options;

using var db = new OptionsContext(options);
```

Читання рядка підключення через `ConfigurationBuilder` (у нашому уроці — з JSON-потоку в пам'яті, щоб не створювати зайві файли):

```csharp
IConfiguration config = new ConfigurationBuilder()
    .AddJsonStream(jsonStream)
    .Build();

var connStr = config.GetConnectionString("DefaultConnection");
```

## Особливості роботи в SQLite

- При використанні файлу `appsettings.json` у проєктах Visual Studio обов'язково встановлюйте властивість файлу `Copy to Output Directory = Copy if newer`, інакше `Directory.GetCurrentDirectory()` або `AppContext.BaseDirectory` не знайде конфігурацію.
- Якщо в рядку підключення SQLite вказується Windows-шлях зі зворотними слешами (наприклад, `C:\Data\app.db`), у JSON його потрібно екранувати як `\\`.

## Підсумок та підводні камені

- Якщо в контексті одночасно перевизначено `OnConfiguring` і передається `DbContextOptions<T>` через конструктор, `OnConfiguring` усе одно викликається після конструктора. У такому разі варто перевіряти `if (!optionsBuilder.IsConfigured)` перед викликом `UseSqlite(...)`.
- Для ASP.NET Core зазвичай використовують реєстрацію `builder.Services.AddDbContext<ApplicationContext>(...)` і конструктор із `DbContextOptions<ApplicationContext>`.
- Ніколи не комітьте паролі чи секретні рядки підключення в Git — для локальної розробки використовуйте User Secrets або змінні середовища.
