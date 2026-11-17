# 2.17. Налаштування індексів

- **Глава:** 03. Створення моделей в EF Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/2.17.php](https://metanit.com/sharp/efcore/2.17.php)
- **Код прикладу:** [`Lesson_2_17_Indexes.cs`](../../src/03_ModelsAndMapping/Lesson_2_17_Indexes.cs)

## Мета уроку

Навчитися створювати індекси в базі даних для прискорення пошуку та забезпечення унікальності даних за допомогою атрибута `[Index]` і Fluent API (`HasIndex`, `IsUnique`, `HasDatabaseName`, `HasFilter`).

## Теоретичний матеріал

За замовчуванням EF Core створює індекси лише для зовнішніх ключів. Для інших стовпців індекси можна додати двома способами:

### 1. Анотації даних (`[Index]`)

Атрибут `[Index]` (з простору імен `Microsoft.EntityFrameworkCore`) ставиться над класом сутності:
- Простий індекс: `[Index(nameof(PhoneNumber))]`
- Складений індекс: `[Index(nameof(PhoneNumber), nameof(Passport))]`
- Унікальний індекс із власним іменем: `[Index(nameof(PhoneNumber), IsUnique = true, Name = "Phone_Index")]`

### 2. Fluent API

```csharp
modelBuilder.Entity<User>()
    .HasIndex(u => u.Passport)
    .IsUnique()
    .HasDatabaseName("IX_Users_Passport_Unique");
```

### Фільтровані (часткові) індекси (`HasFilter`)

Фільтрований індекс індексує лише ті рядки, які задовольняють певну SQL-умову. Наприклад, можна зробити `PhoneNumber` унікальним лише тоді, коли він не дорівнює `NULL`:

```csharp
modelBuilder.Entity<User>()
    .HasIndex(u => u.PhoneNumber)
    .IsUnique()
    .HasFilter("\"PhoneNumber\" IS NOT NULL");
```

## Ключові фрагменти коду

```csharp
[Index(nameof(PhoneNumber), Name = "Attr_Phone_Index")]
[Index(nameof(PhoneNumber), nameof(Passport))]
public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Passport { get; set; }
    public string? PhoneNumber { get; set; }
}
```

## Особливості роботи в SQLite

- SQLite підтримує як звичайні, так і складені та часткові (фільтровані) індекси (`CREATE UNIQUE INDEX ... WHERE "PhoneNumber" IS NOT NULL`).
- **Синтаксис ідентифікаторів у `HasFilter`:** На Metanit для SQL Server часто використовують квадратні дужки `[PhoneNumber] IS NOT NULL`. У SQLite краще використовувати стандартні подвійні лапки `"PhoneNumber" IS NOT NULL` (хоча SQLite також розуміє квадратні дужки для сумісності).

## Підсумок та підводні камені

- На відміну від `HasAlternateKey`, унікальний індекс (`HasIndex(...).IsUnique()`) **не робить** стовпець `NOT NULL` і дозволяє оновлювати його значення.
- У SQLite два значення `NULL` за стандартом SQL вважаються різними, але в SQL Server унікальний індекс без фільтра дозволяє лише один `NULL` — тому `HasFilter` є стандартним способом вирішення цієї задачі.
