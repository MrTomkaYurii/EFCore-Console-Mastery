# 3.7. Власні типи (Owned Types)

- **Глава:** 04. Відносини між моделями
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/3.7.php](https://metanit.com/sharp/efcore/3.7.php)
- **Код прикладу:** [Lesson_3_7_OwnedTypes.cs](../../src/04_Relations/Lesson_3_7_OwnedTypes.cs)

## Мета уроку

Навчитися моделювати **власні типи (Owned Entity Types)** через атрибут `[Owned]` та Fluent API `OwnsOne()`, розуміти, як вони відображаються в таблицю власника і чим відрізняються від звичайних зв'язаних сутностей.

## Теоретичний матеріал

**Власний тип (Owned Type)** — це тип даних, який логічно належить іншій сутності-власнику (Owner) і не може існувати без неї. У DDD це наближено до концепції *Value Object* (об'єкт-значення).

### Ключові характеристики Owned Types

- **Не мають власного первинного ключа** (або використовують ключ власника як тіньовий).
- **Не мають окремого `DbSet<T>`** у контексті даних.
- За замовчуванням **усі властивості власного типу зберігаються в тій самій таблиці**, що й сутність-власник, із префіксом `<НазваВластивості>_<НазваПоля>` (наприклад, `Profile_Name`, `Profile_Age`, `Address_City`).
- **Автоматичне завантаження:** при читанні сутності-власника (наприклад, `db.Users.ToList()`) власні типи завантажуються **без виклику `Include()`**.

### Способи оголошення

1. Атрибут `[Owned]` над залежним класом (`[Owned] public class UserProfile`).
2. Метод Fluent API `modelBuilder.Entity<User>().OwnsOne(u => u.Address);` (також підтримує `OwnsMany()` для колекцій власних типів, які тоді виносяться в окрему таблицю).

## Ключові фрагменти коду

```csharp
public class User
{
    public int Id { get; set; }
    public string? Login { get; set; }
    public UserProfile? Profile { get; set; } // через [Owned]
    public Address? Address { get; set; }     // через OwnsOne()
}

[Owned]
public class UserProfile
{
    public string? Name { get; set; }
    public int Age { get; set; }
}
```

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>().OwnsOne(u => u.Address);
}
```

## Особливості роботи в SQLite

- Для таблиці `Users` у SQLite генеруються стовпці `"Profile_Name" TEXT NULL`, `"Profile_Age" INTEGER NULL`, `"Address_City" TEXT NULL`, `"Address_Street" TEXT NULL`.
- Зверніть увагу: навіть якщо `Age` у C# має тип `int` (non-nullable), у таблиці БД стовпець `Profile_Age` може бути `INTEGER NULL`, якщо сама властивість `Profile` є nullable (`UserProfile?`) — це дозволяє зберігати `User` із `Profile == null`.

## Підсумок та підводні камені

- **Один екземпляр Owned-типу не можна розділити між двома власниками!** Якщо присвоїти один і той самий об'єкт `UserProfile` двом різним `User`, EF Core викине помилку відстеження (або перезапише власника). Для спільних об'єктів-значень використовуйте `[ComplexType]` (урок 3.12).
- Оскільки Owned-типи зберігаються в одній таблиці з власником, їх не можна запитувати напряму через `db.Set<UserProfile>()`.
