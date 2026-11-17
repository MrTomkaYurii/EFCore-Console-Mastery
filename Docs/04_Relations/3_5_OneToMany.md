# 3.5. Відношення один до багатьох

- **Глава:** 04. Відносини між моделями
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/3.5.php](https://metanit.com/sharp/efcore/3.5.php)
- **Код прикладу:** [Lesson_3_5_OneToMany.cs](../../src/04_Relations/Lesson_3_5_OneToMany.cs)

## Мета уроку

Закріпити повний цикл CRUD-операцій (створення, читання з обох сторін, редагування та видалення) для найпоширенішого типу зв'язків у реляційних БД — **один до багатьох (One-to-Many)**.

## Теоретичний матеріал

**Відношення один до багатьох (1:N)** описує ситуацію, коли одна головна сутність (`Company`) може бути пов'язана з багатьма залежними сутностями (`User`), а кожна залежна сутність — лише з однією головною.

### Основні операції зі зв'язком 1:N

1. **Створення:** можна додавати `User` із встановленою властивістю `Company = microsoft`, або додавати користувачів у колекцію `microsoft.Users.Add(...)`.
2. **Читання:**
   - З боку залежної сутності: `db.Users.Include(u => u.Company)`
   - З боку головної сутності: `db.Companies.Include(c => c.Users)`
3. **Редагування (перенесення в іншу категорію/компанію):** достатньо змінити `user.Company = newCompany;` (або `user.CompanyId = newCompany.Id;`) і викликати `db.SaveChanges()`.
4. **Видалення:** видалення окремого `User` не зачіпає `Company`, але видалення `Company` при обов'язковому зв'язку (`int CompanyId`) каскадно видалить усіх її користувачів.

## Ключові фрагменти коду

```csharp
public class Company
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public List<User> Users { get; set; } = new();
}

public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
}
```

```csharp
// Переведення співробітника в іншу компанію
var bob = db.Users.First(u => u.Name == "Bob");
bob.Company = google;
db.SaveChanges();
```

## Особливості роботи в SQLite

- У SQLite зв'язок 1:N створює індекс `IX_Users_CompanyId` для зовнішнього ключа, що прискорює `JOIN`-и та каскадні операції.
- Поведінка CRUD-операцій у SQLite повністю ідентична SQL Server.

## Підсумок та підводні камені

- Завжди ініціалізуйте колекційну навігаційну властивість (`= new();`), щоб уникнути `NullReferenceException` при додаванні елементів у пам'яті.
- При переведенні об'єкта в іншу компанію через `user.Company = otherCompany;` у відстежуваному контексті EF Core сам оновить `CompanyId` та колекції `Users` обох компаній.
