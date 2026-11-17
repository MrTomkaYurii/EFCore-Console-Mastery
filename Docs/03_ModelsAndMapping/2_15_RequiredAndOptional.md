# 2.15. Обов'язкові та необов'язкові властивості

- **Глава:** 03. Створення моделей в EF Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/2.15.php](https://metanit.com/sharp/efcore/2.15.php)
- **Код прикладу:** [`Lesson_2_15_RequiredAndOptional.cs`](../../src/03_ModelsAndMapping/Lesson_2_15_RequiredAndOptional.cs)

## Мета уроку

Зрозуміти, як EF Core визначає обов'язкові (`NOT NULL`) та необов'язкові (`NULL`) стовпці на основі системи типів C# (Nullable Reference Types і `Nullable<T>`), атрибута `[Required]` та методу Fluent API `IsRequired()`.

## Теоретичний матеріал

### Поведінка за замовчуванням (Conventions)

Коли в проєкті увімкнено `<Nullable>enable</Nullable>`:
- **Обов'язкові (`NOT NULL`):** усі не-nullable типи (`int`, `bool`, `DateTime`, `string`).
- **Необов'язкові (`NULL`):** усі nullable-типи (`int?`, `string?`).

### Явне налаштування обов'язковості

Навіть якщо властивість оголошена як `string?`, її можна зробити обов'язковою в базі даних:
- **Анотація даних:** `[Required]`
- **Fluent API:** `modelBuilder.Entity<User>().Property(u => u.Login).IsRequired();`

І навпаки, якщо потрібно зробити не-nullable властивість необов'язковою у БД, у Fluent API використовують `.IsRequired(false)`.

## Ключові фрагменти коду

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // NOT NULL за типом
    [Required]
    public string? Email { get; set; }               // NOT NULL через [Required]
    public string? Login { get; set; }               // NOT NULL через IsRequired()
    public string? Company { get; set; }             // NULL дозволено
}
```

## Особливості роботи в SQLite

- SQLite повністю підтримує обмеження `NOT NULL` на рівні DDL (`"Email" TEXT NOT NULL`).
- При спробі зберегти `null` у стовпець із `NOT NULL` SQLite повертає помилку `SQLite Error 19: 'NOT NULL constraint failed: ...'`, яку EF Core обгортає в `DbUpdateException`.

## Підсумок та підводні камені

- У C# властивість `string? Email` з `[Required]` все одно може містити `null` у пам'яті до виклику `SaveChanges()` — перевірка на рівні БД спрацює лише під час збереження.
- Для значень за замовчуванням у не-nullable рядках (`public string Name { get; set; } = "";`) пам'ятайте, що порожній рядок `""` **не є `NULL`**, тому БД пропустить його навіть із `NOT NULL`.
