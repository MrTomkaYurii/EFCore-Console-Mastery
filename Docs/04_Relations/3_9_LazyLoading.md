# 3.9. Ліниве завантаження (Lazy loading)

- **Глава:** 04. Відносини між моделями
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/3.9.php](https://metanit.com/sharp/efcore/3.9.php)
- **Код прикладу:** [Lesson_3_9_LazyLoading.cs](../../src/04_Relations/Lesson_3_9_LazyLoading.cs)

## Мета уроку

Зрозуміти принцип роботи **лінивого завантаження (Lazy Loading)** через динамічні проксі-класи (`Microsoft.EntityFrameworkCore.Proxies`), умови його налаштування та ризики продуктивності (проблема N+1).

## Теоретичний матеріал

**Lazy Loading (ліниве завантаження)** — це автоматичне, неявне завантаження пов'язаних даних із бази даних у момент першого звернення до навігаційної властивості в коді C#.

### Обов'язкові умови для проксі-підходу

1. Встановити NuGet-пакет `Microsoft.EntityFrameworkCore.Proxies`.
2. У методі `OnConfiguring` викликати `.UseLazyLoadingProxies()` перед або разом із `.UseSqlite(...)`.
3. Усі класи сутностей мають бути **відкритими для наслідування** (`public` і не `sealed`), оскільки EF Core динамічно генерує похідні проксі-класи в runtime.
4. Усі навігаційні властивості мають бути оголошені з модифікатором `virtual` (`public virtual Company? Company { get; set; }`).

### Як це працює під капотом

Коли ви виконуєте `db.Users.ToList()`, у пам'ять завантажуються лише рядки з таблиці `Users`. Кожен об'єкт насправді є проксі-екземпляром. При зверненні до `user.Company` проксі перевіряє, чи завантажена ця компанія:
- Якщо її ще немає в поточному `DbContext` — виконується додатковий SQL-запит `SELECT ... FROM "Companies" WHERE "Id" = @__p_0`.
- Якщо компанія вже є в контексті (наприклад, її завантажив попередній користувач) — повторний SQL-запит **не виконується**, об'єкт береться з кешу контексту.

## Ключові фрагменти коду

```csharp
public class Company
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public virtual List<User> Users { get; set; } = new();
}

public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int? CompanyId { get; set; }
    public virtual Company? Company { get; set; }
}
```

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder
        .UseLazyLoadingProxies()
        .UseSqlite(SqliteDbHelper.GetConnectionString("ch04_l3_9"));
}
```

## Особливості роботи в SQLite

- У нашому проєкті пакет `Microsoft.EntityFrameworkCore.Proxies` версії `11.0.0` уже підключений у `.csproj`, тому приклади працюють «з коробки» на SQLite.
- Оскільки SQLite є локальною файловою БД, накладні витрати на мережеві затримки відсутні, тому проблема N+1 може бути менш помітною на малих даних, але на реальних базах (або під навантаженням) десятки зайвих `SELECT`-ів суттєво сповільнюють роботу.

## Підсумок та підводні камені

- **Проблема N+1 запитів:** якщо у вас 100 користувачів у 100 різних компаніях, цикл із зверненням до `user.Company?.Name` виконає `1 + 100 = 101` SQL-запит замість 1 запиту з `Include()`.
- **Закритий `DbContext`:** спроба звернутися до незавантаженої `virtual`-властивості після виходу з блоку `using (var db = ...)` призведе до `ObjectDisposedException` (або поверне `null` залежно від налаштувань), оскільки проксі більше не має активного контексту.
- **Серіалізація в JSON (API):** серіалізатори (наприклад, `System.Text.Json`) при обході всіх властивостей об'єкта можуть спровокувати лавиноподібне ліниве завантаження всього графа БД та циклічні посилання (`Company -> Users -> Company -> ...`).
