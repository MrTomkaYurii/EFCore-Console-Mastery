# 3.2. Каскадне видалення

- **Глава:** 04. Відносини між моделями
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/3.2.php](https://metanit.com/sharp/efcore/3.2.php)
- **Код прикладу:** [Lesson_3_2_CascadeDelete.cs](../../src/04_Relations/Lesson_3_2_CascadeDelete.cs)

## Мета уроку

Розібратися, як у EF Core працює каскадне видалення залежних сутностей при видаленні головної сутності та як керувати цією поведінкою через типізацію ключів і `DeleteBehavior`.

## Теоретичний матеріал

**Каскадне видалення (Cascade Delete)** — механізм, за якого видалення головної (principal) сутності автоматично призводить до видалення всіх пов'язаних із нею залежних (dependent) сутностей.

Поведінка EF Core за замовчуванням залежить від **обов'язковості зв'язку**:

- **Обов'язковий зв'язок** (`int CompanyId`, або `[Required]` на навігаційній властивості, або non-nullable навігаційна властивість без FK) → `DeleteBehavior.Cascade` (`ON DELETE CASCADE`).
- **Необов'язковий зв'язок** (`int? CompanyId` або nullable навігаційна властивість без FK) → `DeleteBehavior.ClientSetNull` (зовнішній ключ стає `NULL`, якщо залежні сутності відстежуються контекстом; у БД обмеження має `ON DELETE NO ACTION` / `RESTRICT`).

### Основні значення `DeleteBehavior` у Fluent API

| Значення `DeleteBehavior` | Дія в БД / EF Core при видаленні головної сутності |
|---|---|
| `Cascade` | Видаляє залежні сутності як у БД (`ON DELETE CASCADE`), так і в пам'яті контексту |
| `SetNull` | Встановлює FK у `NULL` безпосередньо на рівні БД (`ON DELETE SET NULL`) |
| `Restrict` | Забороняє видалення головної сутності, якщо існують залежні записи |
| `ClientSetNull` | Встановлює FK у `NULL` лише для відстежуваних об'єктів у контексті |
| `NoAction` | Не виконує жодних дій у БД; за наявності дочірніх записів БД поверне помилку FK |

## Ключові фрагменти коду

```csharp
// Обов'язковий зв'язок -> каскадне видалення за замовчуванням
public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int CompanyId { get; set; } // Не nullable -> ON DELETE CASCADE
    public Company? Company { get; set; }
}

// Необов'язковий зв'язок -> можна налаштувати ON DELETE SET NULL
public class Contractor
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
}
```

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Contractor>()
        .HasOne(c => c.Company)
        .WithMany(comp => comp.Contractors)
        .HasForeignKey(c => c.CompanyId)
        .OnDelete(DeleteBehavior.SetNull);
}
```

## Особливості роботи в SQLite

- У SQL Server часто виникає помилка *«Introducing FOREIGN KEY constraint ... may cause cycles or multiple cascade paths»*, коли кілька шляхів каскадного видалення ведуть до однієї таблиці. SQLite **дозволяє** множинні каскадні шляхи без такого обмеження, але все одно не допускає циклів у самопосилальних таблицях.
- Для `DeleteBehavior.SetNull` SQLite коректно генерує `ON DELETE SET NULL` у `CREATE TABLE`.
- Якщо використовувати `ExecuteDeleteAsync()` (масове видалення в обхід Change Tracker), спрацюють лише правила, задані на рівні самої БД (`Cascade`, `SetNull`), а не клієнтські (`ClientSetNull`, `ClientCascade`).

## Підсумок та підводні камені

- Навіть якщо навігаційна властивість `Company?` є nullable, але зовнішній ключ `int CompanyId` **не nullable**, зв'язок усе одно вважається обов'язковим і застосовується `ON DELETE CASCADE`.
- При використанні `ClientSetNull` (за замовчуванням для `int?`), якщо залежні сутності **не завантажені** в поточний `DbContext`, спроба видалити головну сутність призведе до `DbUpdateException` через порушення обмеження зовнішнього ключа в БД. Щоб уникнути цього, вказуйте `.OnDelete(DeleteBehavior.SetNull)` явно.
