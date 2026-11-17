# 3.4. Відношення один до одного

- **Глава:** 04. Відносини між моделями
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/3.4.php](https://metanit.com/sharp/efcore/3.4.php)
- **Код прикладу:** [Lesson_3_4_OneToOne.cs](../../src/04_Relations/Lesson_3_4_OneToOne.cs)

## Мета уроку

Навчитися моделювати та виконувати CRUD-операції для відношення **один до одного (One-to-One)** у EF Core на прикладі пари `User` та `UserProfile`.

## Теоретичний матеріал

**Відношення один до одного (1:1)** означає, що один екземпляр головної сутності може бути пов'язаний щонайбільше з одним екземпляром залежної сутності, і навпаки. Типовий сценарій — винесення рідко використовуваних або конфіденційних полів (наприклад, профілю користувача `UserProfile`) в окрему таблицю від облікового запису (`User`).

### Як EF Core розрізняє головну та залежну сутності у зв'язку 1:1

- У головної сутності (`User`) є лише посилальна навігаційна властивість `UserProfile? Profile` (без зовнішнього ключа).
- У залежної сутності (`UserProfile`) є і навігаційна властивість `User? User`, і **зовнішній ключ** `int UserId`.
- На рівні БД у таблиці `UserProfiles` для стовпця `UserId` автоматично створюється **унікальний індекс** (`CREATE UNIQUE INDEX "IX_UserProfiles_UserId" ON "UserProfiles" ("UserId")`), що на рівні СУБД гарантує неможливість прив'язки двох профілів до одного користувача.

### Що якщо зовнішнього ключа `UserId` немає в жодному з класів?

Якщо жодна зі сторін не містить властивості зовнішнього ключа, EF Core не зможе сам визначити, хто є головним, а хто залежним, і потребуватиме явної конфігурації у Fluent API:
```csharp
modelBuilder.Entity<User>()
    .HasOne(u => u.Profile)
    .WithOne(p => p.User)
    .HasForeignKey<UserProfile>(p => p.UserId);
```

## Ключові фрагменти коду

```csharp
public class User
{
    public int Id { get; set; }
    public string? Login { get; set; }
    public string? Password { get; set; }
    public UserProfile? Profile { get; set; }
}

public class UserProfile
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Age { get; set; }
    public int UserId { get; set; } // Унікальний FK вказує, що UserProfile — залежна сутність
    public User? User { get; set; }
}
```

```csharp
// Читання з підвантаженням профілю
var users = db.Users.Include(u => u.Profile).ToList();
```

## Особливості роботи в SQLite

- У SQLite зв'язок 1:1 реалізується через таблицю `UserProfiles` із `FOREIGN KEY("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE` та окремим `CREATE UNIQUE INDEX "IX_UserProfiles_UserId" ON "UserProfiles" ("UserId")`.
- При спробі додати другий `UserProfile` з тим самим `UserId` SQLite викине `SqliteException: SQLite Error 19: 'UNIQUE constraint failed: UserProfiles.UserId'`.

## Підсумок та підводні камені

- При видаленні головної сутності (`User`) залежний `UserProfile` видаляється каскадно, якщо `UserId` має тип `int` (non-nullable).
- Якщо ви хочете, щоб `User` і `UserProfile` фізично зберігалися в **одній** таблиці БД (Table Splitting), використовуйте спільний первинний ключ і `ToTable("Users")` у Fluent API, або ж розгляньте **Owned Types** (урок 3.7).
