# 2.9. Обмеження властивостей

- **Глава:** 03. Створення моделей в EF Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/2.9.php](https://metanit.com/sharp/efcore/2.9.php)
- **Код прикладу:** [`Lesson_2_9_PropertyConstraints.cs`](../../src/03_ModelsAndMapping/Lesson_2_9_PropertyConstraints.cs)

## Мета уроку

Навчитися задавати обмеження на значення стовпців за допомогою `CHECK`-обмежень (`HasCheckConstraint`) та обмежувати максимальну довжину рядків і масивів байтів (`[MaxLength]`, `HasMaxLength`), розуміючи різницю в їхній підтримці між SQL Server та SQLite.

## Теоретичний матеріал

### 1. Обмеження `CHECK` (`HasCheckConstraint`)

Метод `HasCheckConstraint` викликається всередині `ToTable(...)` і додає до DDL таблиці умову `CONSTRAINT "..." CHECK(...)`:

```csharp
modelBuilder.Entity<User>()
    .ToTable(t => t.HasCheckConstraint("ValidAge", "Age > 0 AND Age < 120"));
```

Якщо під час `INSERT` або `UPDATE` умова порушується, СУБД відхиляє операцію, а EF Core викидає `DbUpdateException`.

### 2. Обмеження довжини (`[MaxLength]` та `HasMaxLength`)

Застосовується лише до `string` та `byte[]`:
- **Анотація даних:** `[MaxLength(50)]` (також є `[MinLength]`, який використовується валідаторами на рівні програми, але **не впливає** на схему БД).
- **Fluent API:** `modelBuilder.Entity<User>().Property(u => u.Email).HasMaxLength(50);`

## Ключові фрагменти коду

```csharp
modelBuilder.Entity<User>()
    .ToTable(t => t.HasCheckConstraint("ValidAge", "Age > 0 AND Age < 120"));

modelBuilder.Entity<User>()
    .Property(u => u.Email)
    .HasMaxLength(50);
```

## Особливості роботи в SQLite

- **`HasCheckConstraint`:** Повністю підтримується в SQLite! У DDL генерується `CONSTRAINT "ValidAge" CHECK(Age > 0 AND Age < 120)`, і спроба вставити `Age = 123` викликає `SQLite Error 19: 'CHECK constraint failed: ValidAge'`.
- **`MaxLength` / `HasMaxLength`:** **SQLite не обмежує довжину рядків** на рівні типу даних (усі рядки зберігаються як `TEXT` довільної довжини). На відміну від SQL Server (`nvarchar(50)`), де перевищення довжини викличе помилку, SQLite спокійно збереже рядок будь-якої довжини.

## Підсумок та підводні камені

- Для гарантованої перевірки довжини рядка в застосунках із SQLite використовуйте серверну валідацію (наприклад, `Validator.TryValidateObject` або FluentValidation) перед викликом `SaveChanges()`.
- Після перехоплення `DbUpdateException` не забувайте очищати трекер змін (`db.ChangeTracker.Clear()`) або виправляти невалідну сутність, інакше наступний `SaveChanges()` повторно спробує зберегти її.
