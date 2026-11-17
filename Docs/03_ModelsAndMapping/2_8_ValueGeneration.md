# 2.8. Генерація значень властивостей і стовпців

- **Глава:** 03. Створення моделей в EF Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/2.8.php](https://metanit.com/sharp/efcore/2.8.php)
- **Код прикладу:** [`Lesson_2_8_ValueGeneration.cs`](../../src/03_ModelsAndMapping/Lesson_2_8_ValueGeneration.cs)

## Мета уроку

Навчитися керувати генерацією значень на рівні бази даних: вмикати та вимикати автогенерацію первинних ключів, задавати значення за замовчуванням (`HasDefaultValue`, `HasDefaultValueSql`) та створювати обчислювані стовпці (`HasComputedColumnSql`).

## Теоретичний матеріал

### 1. Генерація первинних ключів

За замовчуванням для первинних ключів типу `int` або `Guid` база даних (або EF Core) автоматично генерує нове значення під час `SaveChanges()`.

Щоб **вимкнути** автогенерацію (коли ключі задаються вручну):
- **Анотація даних:** `[DatabaseGenerated(DatabaseGeneratedOption.None)]`
- **Fluent API:** `.Property(u => u.Id).ValueGeneratedNever()`

### 2. Значення за замовчуванням

- **`HasDefaultValue(18)`** — задає константне значення за замовчуванням у DDL (`DEFAULT 18`).
- **`HasDefaultValueSql("...")`** — задає SQL-вираз, який виконує СУБД при вставці рядка.

### 3. Обчислювані стовпці

Метод `HasComputedColumnSql("...")` створює стовпець, значення якого обчислюється самою СУБД на основі інших стовпців рядка:

```csharp
modelBuilder.Entity<User>()
    .Property(u => u.FullName)
    .HasComputedColumnSql("FirstName || ' ' || LastName");
```

## Ключові фрагменти коду

```csharp
modelBuilder.Entity<User>()
    .Property(u => u.Age)
    .HasDefaultValue(18);

modelBuilder.Entity<User>()
    .Property(u => u.CreatedAt)
    .HasDefaultValueSql("DATETIME('now')");
```

## Особливості роботи в SQLite

| Функціональність | SQL Server (T-SQL) | SQLite |
| :--- | :--- | :--- |
| Поточна дата/час у `HasDefaultValueSql` | `GETDATE()` або `SYSDATETIME()` | `DATETIME('now')` або `CURRENT_TIMESTAMP` |
| Конкатенація в `HasComputedColumnSql` | `FirstName + ' ' + LastName` | `FirstName \|\| ' ' \|\| LastName` |
| Обчислювані стовпці | Підтримуються з коробки | Підтримуються з SQLite 3.31+ (`GENERATED ALWAYS AS (...) VIRTUAL`) |

## Підсумок та підводні камені

- Якщо для властивості `int Age` задано `HasDefaultValue(18)`, але в C# ви створили об'єкт без ініціалізації `Age` (тобто `Age == 0`), EF Core розпізнає `0` як значення за замовчуванням CLR і **не передасть** його в `INSERT`, дозволивши БД підставити `18`. Проте якщо ви явно передасте `Age = 0` для `int?`, краще перевірити поведінку `ValueGeneratedOnAdd` або використовувати `int` із `HasDefaultValue`.
- Якщо вимкнути автогенерацію ключа (`ValueGeneratedNever`), але не задати `Id` при додаванні двох об'єктів, другий об'єкт також отримає `Id = 0`, і виникне помилка порушення унікальності `PRIMARY KEY`.
