# 3.10. Налаштування зовнішнього ключа через анотації та Fluent API

- **Глава:** 04. Відносини між моделями
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/3.10.php](https://metanit.com/sharp/efcore/3.10.php)
- **Код прикладу:** [Lesson_3_10_ForeignKeyConfig.cs](../../src/04_Relations/Lesson_3_10_ForeignKeyConfig.cs)

## Мета уроку

Навчитися явно задавати властивість зовнішнього ключа (коли її ім'я не відповідає стандартним конвенціям), а також прив'язувати зовнішній ключ не до первинного ключа, а до іншої унікальної властивості головної сутності через `HasPrincipalKey`.

## Теоретичний матеріал

Якщо ім'я властивості зовнішнього ключа відрізняється від `<NavProperty>Id` (наприклад, `CompanyInfoKey`), EF Core не розпізнає його автоматично і створить додатковий тіньовий стовпець `CompanyId`. Щоб уникнути цього, використовують два підходи:

1. **Атрибут `[ForeignKey]` (Data Annotations)** — розміщується над навігаційною властивістю або над самим зовнішнім ключем і вказує ім'я парної властивості.
2. **Fluent API (`HasOne / HasMany / WithOne / WithMany / HasForeignKey`)** — конфігурується в методі `OnModelCreating(ModelBuilder)`.

### Порівняння підходів

| Підхід | Переваги | Недоліки |
|---|---|---|
| `[ForeignKey("CompanyInfoKey")]` | Лаконічно, видно прямо в моделі | Не підтримує `HasPrincipalKey` або складні налаштування `OnDelete` |
| Fluent API (`HasForeignKey` + `HasPrincipalKey`) | Повний контроль, не забруднює доменні моделі атрибутами | Більше коду в `DbContext` |

### Альтернативний головний ключ (`HasPrincipalKey`)

За замовчуванням зовнішній ключ посилається на первинний ключ головної сутності (`Id`). Проте метод `.HasPrincipalKey(c => c.Name)` дозволяє прив'язати FK до будь-якої іншої властивості (для неї EF Core автоматично створить `UNIQUE` обмеження в БД).

## Ключові фрагменти коду

```csharp
public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int? CompanyInfoKey { get; set; }

    [ForeignKey(nameof(CompanyInfoKey))]
    public Company? Company { get; set; }
}
```

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Налаштування нестандартного FK через Fluent API
    modelBuilder.Entity<User>()
        .HasOne(u => u.Company)
        .WithMany(c => c.Users)
        .HasForeignKey(u => u.CompanyInfoKey);

    // Прив'язка FK до альтернативного ключа (Company.Name)
    modelBuilder.Entity<Employee>()
        .HasOne(e => e.Company)
        .WithMany(c => c.Employees)
        .HasForeignKey(e => e.CompanyName)
        .HasPrincipalKey(c => c.Name);
}
```

## Особливості роботи в SQLite

- При використанні `.HasPrincipalKey(c => c.Name)` у SQLite для таблиці `Companies` автоматично генерується обмеження `CONSTRAINT "AK_Companies_Name" UNIQUE("Name")`, а для `Employees` — `FOREIGN KEY("CompanyName") REFERENCES "Companies"("Name")`.
- У SQLite не можна легко змінити `FOREIGN KEY` або `UNIQUE` обмеження через `ALTER TABLE` у міграціях — EF Core для цього перестворює таблицю з копіюванням даних.
- Рядкові ключі в SQLite за замовчуванням порівнюються з урахуванням регістру (`BINARY` collation), на відміну від SQL Server, де зазвичай використовується case-insensitive collation (`CI`).

## Підсумок та підводні камені

- Якщо одночасно вказати і `[ForeignKey]`, і Fluent API `HasForeignKey()`, пріоритет має Fluent API.
- Для властивості, обраної як `PrincipalKey`, усі значення в таблиці головної сутності **повинні бути унікальними** та не можуть бути `NULL` (інакше БД відхилить вставку).
- Використання nullable типу (`int? CompanyInfoKey`) робить зв'язок необов'язковим, що змінює поведінку видалення за замовчуванням із `Cascade` на `ClientSetNull`.
