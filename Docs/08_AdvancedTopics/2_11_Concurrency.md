# 2.11. Паралелізм та оптимістичне блокування (Concurrency)

- **Глава:** 08. Додаткові теми
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/2.11.php](https://metanit.com/sharp/efcore/2.11.php)
- **Код прикладу:** [Lesson_2_11_Concurrency.cs](../../src/08_AdvancedTopics/Lesson_2_11_Concurrency.cs)

---

## Мета уроку

Навчитися захищати дані від втрати оновлень (*lost update*) у багатокористувацькому середовищі за допомогою механізму **оптимістичного блокування (Optimistic Concurrency)** в Entity Framework Core, налаштовувати токени паралелізму через `[ConcurrencyCheck]` та `IsConcurrencyToken()`, а також обробляти виняток `DbUpdateConcurrencyException`.

---

## Теоретичний матеріал

Коли кілька користувачів або процесів одночасно читають і змінюють один і той самий запис у БД, виникає проблема паралелізму: другий користувач може перезаписати зміни першого, навіть не знаючи про них.

EF Core використовує підхід **оптимістичного паралелізму**:
- Записи в БД **не блокуються** на рівні транзакцій під час читання.
- Перед збереженням (`SaveChanges`) EF Core перевіряє, чи не змінився спеціальний **токен паралелізму (Concurrency Token)** з моменту завантаження сутності.
- Для цього у SQL-команди `UPDATE` та `DELETE` додається додаткова умова `WHERE [ConcurrencyToken] = @originalValue`.
- Якщо інший користувач вже оновив рядок, кількість змінених рядків дорівнює `0`, і EF Core викидає виняток `DbUpdateConcurrencyException`.

### Способи налаштування токенів паралелізму

| Підхід | Атрибут / Метод | Опис |
| :--- | :--- | :--- |
| Анотація даних | `[ConcurrencyCheck]` | Позначає конкретну властивість (наприклад, `Name`) як токен паралелізму. |
| Fluent API | `.Property(x => x.Prop).IsConcurrencyToken()` | Налаштовує токен паралелізму у `OnModelCreating` без забруднення моделі атрибутами. |
| Версія рядка (SQL Server) | `[Timestamp]` / `.IsRowVersion()` | Автоматично генерований БД стовпець `byte[]` (`rowversion`), який змінюється при кожному `INSERT`/`UPDATE`. |

### Стратегії розв'язання `DbUpdateConcurrencyException`

Коли виникає конфлікт, у блоці `catch (DbUpdateConcurrencyException ex)` можна отримати об'єкт `EntityEntry` (`ex.Entries.Single()`) і застосувати одну з трьох стратегій:
1. **Database Wins (перемагає БД):** завантажити актуальні значення через `entry.GetDatabaseValues()` і оновити `CurrentValues` та `OriginalValues`.
2. **Client Wins (перемагає клієнт):** оновити лише `entry.OriginalValues.SetValues(databaseValues)` і повторно викликати `SaveChanges()`, щоб перезаписати БД даними поточного клієнта.
3. **Custom Merge (злиття):** порівняти `OriginalValues`, `CurrentValues` і `DatabaseValues` по кожному полю та сформувати об'єднаний результат.

---

## Ключові фрагменти коду

Налаштування токена паралелізму через атрибут та Fluent API:

```csharp
public class User
{
    public int Id { get; set; }

    [ConcurrencyCheck]
    public string? Name { get; set; }

    public int Version { get; set; }
}

// У OnModelCreating:
modelBuilder.Entity<User>()
    .Property(u => u.Version)
    .IsConcurrencyToken();
```

Автоматичне оновлення токена версії (`Guid`) у `SaveChanges()` для SQLite:

```csharp
public override int SaveChanges()
{
    foreach (var entry in ChangeTracker.Entries<Document>())
    {
        if (entry.State is EntityState.Added or EntityState.Modified)
        {
            entry.Entity.RowVersion = Guid.NewGuid();
        }
    }
    return base.SaveChanges();
}
```

Перехоплення та розв'язання конфлікту:

```csharp
catch (DbUpdateConcurrencyException ex)
{
    var entry = ex.Entries.Single();
    var databaseValues = entry.GetDatabaseValues();
    if (databaseValues is not null)
    {
        // Наприклад, стратегія "Client Wins":
        entry.OriginalValues.SetValues(databaseValues);
        db.SaveChanges();
    }
}
```

---

## Особливості роботи в SQLite

У статті на Metanit для механізму версіонування рядків наведено приклад із `[Timestamp]` та `IsRowVersion()` для **MS SQL Server**:
```csharp
[Timestamp]
public byte[]? Timestamp { get; set; }
```
- **SQL Server** має вбудований тип `rowversion` (`timestamp`), який **автоматично** оновлюється сервером БД при кожному `INSERT` або `UPDATE`.
- **SQLite не підтримує** автоматичну генерацію `rowversion`/`Timestamp`. Якщо просто додати `[Timestamp]` у SQLite, стовпець матиме значення `NULL` або не оновлюватиметься при зміні рядка, через що перевірка паралелізму не працюватиме автоматично.
- **Рішення для SQLite:** використовувати властивість `Guid` або `int` з `.IsConcurrencyToken()` (або `[ConcurrencyCheck]`) та оновлювати її значення у коді застосунку (наприклад, у `SaveChanges()` або `SaveChangesInterceptor`), або ж за допомогою SQLite-тригера `AFTER UPDATE`.

---

## Підсумок та підводні камені

- **Не забувайте оновлювати токен:** якщо ви використовуєте `IsConcurrencyToken()` на `int` або `Guid` у SQLite без перевизначення `SaveChanges()`, значення токена не зміниться, і конфлікт не буде виявлений.
- **`GetDatabaseValues()` повертає `null`**, якщо рядок уже був **видалений** іншим користувачем. Завжди перевіряйте результат на `null` перед викликом `SetValues(...)`.
- **Повторне збереження при "Client Wins":** навіть після `entry.OriginalValues.SetValues(dbValues)` повторний `SaveChanges()` може знову викинути `DbUpdateConcurrencyException`, якщо третій користувач встигне змінити запис у проміжку. У продакшені варто обмежувати кількість спроб (retry loop).
