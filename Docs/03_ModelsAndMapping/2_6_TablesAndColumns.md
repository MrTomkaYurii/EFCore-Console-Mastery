# 2.6. Зіставлення таблиць і стовпців

- **Глава:** 03. Створення моделей в EF Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/2.6.php](https://metanit.com/sharp/efcore/2.6.php)
- **Код прикладу:** [`Lesson_2_6_TablesAndColumns.cs`](../../src/03_ModelsAndMapping/Lesson_2_6_TablesAndColumns.cs)

## Мета уроку

Навчитися перевизначати стандартні імена таблиць і стовпців у базі даних за допомогою анотацій даних (`[Table]`, `[Column]`) та Fluent API (`ToTable()`, `HasColumnName()`).

## Теоретичний матеріал

За замовчуванням кожна сутність зіставляється з таблицею, ім'я якої збігається з назвою властивості `DbSet<T>` у контексті (або з назвою класу, якщо `DbSet<T>` немає), а кожна властивість — з однойменним стовпцем.

### Перевизначення імені таблиці

| Спосіб | Приклад | Примітка |
| :--- | :--- | :--- |
| **Анотація даних** | `[Table("People")]` | Можна також вказати схему: `[Table("People", Schema = "userstore")]` |
| **Fluent API** | `modelBuilder.Entity<User>().ToTable("People", schema: "userstore");` | Має вищий пріоритет за `[Table]` |

### Перевизначення імені стовпця

| Спосіб | Приклад |
| :--- | :--- |
| **Анотація даних** | `[Column("user_id")]` над властивістю |
| **Fluent API** | `modelBuilder.Entity<User>().Property(u => u.Id).HasColumnName("user_id");` |

## Ключові фрагменти коду

```csharp
[Table("People")]
public class UserAnnotated
{
    [Column("user_id")]
    public int Id { get; set; }

    [Column("full_name")]
    public string? Name { get; set; }
}
```

## Особливості роботи в SQLite

- **Схеми таблиць (`schema`):** На відміну від SQL Server чи PostgreSQL, **SQLite не підтримує схеми** всередині одного файлу бази даних. Якщо передати `schema: "userstore"` у `ToTable()`, провайдер SQLite ігнорує схему і створює таблицю просто з іменем `"Employees"` (без префікса `userstore.`).
- Атрибути `[Table]` і `[Column]` та методи `ToTable()` і `HasColumnName()` працюють у SQLite без обмежень.

## Підсумок та підводні камені

- У коді C# ви завжди працюєте з іменами класів і властивостей (`db.Users`, `u.Id`), навіть якщо в БД таблиця називається `People`, а стовпець — `user_id`.
- При написанні «сирих» SQL-запитів (`FromSqlRaw`, `SqlQueryRaw`) потрібно використовувати саме **імена таблиць і стовпців із БД**, а не C#-класів.
