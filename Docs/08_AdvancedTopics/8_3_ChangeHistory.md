# 8.3. Зберігання історії змін (Temporal Tables / SQLite Audit)

- **Глава:** 08. Додаткові теми
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/8.3.php](https://metanit.com/sharp/efcore/8.3.php)
- **Код прикладу:** [Lesson_8_3_ChangeHistory.cs](../../src/08_AdvancedTopics/Lesson_8_3_ChangeHistory.cs)

---

## Мета уроку

Вивчити механізм **темпоральних таблиць (Temporal Tables)** в EF Core для MS SQL Server (`IsTemporal()`) та реалізувати кросплатформну альтернативу для SQLite на основі **таблиці аудиту** та `SaveChangesInterceptor` для відстеження й відновлення історії змін.

---

## Теоретичний матеріал

### 1. Темпоральні таблиці у MS SQL Server (`IsTemporal`)

Починаючи з EF Core 6.0, провайдер **MS SQL Server** підтримує вбудовані системно-версіоновані таблиці (`SYSTEM_VERSIONING = ON`). При налаштуванні:

```csharp
modelBuilder
    .Entity<User>()
    .ToTable("Users", t => t.IsTemporal());
```

SQL Server автоматично створює:
- Два приховані (shadow) стовпці `PeriodStart` і `PeriodEnd` типу `datetime2`.
- Паралельну таблицю історії `UsersHistory`, куди сервер БД автоматично переносить попередні версії рядків при кожному `UPDATE` або `DELETE`.

Таблицю історії та назви стовпців періоду можна кастомізувати:

```csharp
modelBuilder.Entity<User>().ToTable("Users", t => t.IsTemporal(h =>
{
    h.HasPeriodStart("CreatedAt");
    h.HasPeriodEnd("DeletedAt");
    h.UseHistoryTable("PeopleDataHistory");
}));
```

#### Методи вибірки історії у SQL Server

| Метод EF Core | Опис |
| :--- | :--- |
| `TemporalAll()` | Повертає всі версії рядків за весь час (і поточні, і історичні). |
| `TemporalAsOf(date)` | Повертає стан даних на конкретний момент часу `date`. |
| `TemporalFromTo(start, end)` | Повертає стани, що існували в інтервалі `(start, end)`. |
| `TemporalBetween(start, end)` | Аналогічно `TemporalFromTo`, але включає межу `end`. |
| `TemporalContainedIn(start, end)` | Повертає рядки, які почали й завершили існування виключно всередині `[start, end]`. |

Доступ до прихованих стовпців періоду у запитах:
```csharp
var history = db.Users.TemporalAll()
    .Where(u => u.Id == 1)
    .OrderBy(u => EF.Property<DateTime>(u, "PeriodStart"))
    .Select(u => new
    {
        User = u,
        Start = EF.Property<DateTime>(u, "PeriodStart"),
        End = EF.Property<DateTime>(u, "PeriodEnd")
    })
    .ToList();
```

Для відновлення видаленого запису зі збереженням старого `Id` у SQL Server необхідно в межах транзакції увімкнути `SET IDENTITY_INSERT [dbo].[Users] ON`.

### 2. Альтернатива для SQLite: `SaveChangesInterceptor` + таблиця аудиту

Оскільки SQLite **не підтримує** `IsTemporal()`, історію змін у SQLite реалізують одним із двох способів:
1. **SQLite-тригери (`AFTER INSERT / UPDATE / DELETE`)**, які записують старі та нові значення у таблицю `UsersHistory`.
2. **`SaveChangesInterceptor` (або перевизначення `SaveChanges`)**, який читає стан `ChangeTracker` (`EntityState.Added`, `Modified`, `Deleted`) та `OriginalValues` / `CurrentValues`, зберігаючи знімки у таблицю аудиту `UserHistories`.

У нашому уроці реалізовано саме другий підхід — він повністю типобезпечний, працює через LINQ і легко переноситься між будь-якими СУБД.

---

## Ключові фрагменти коду

Перехоплення змін у `SaveChangesInterceptor`:

```csharp
foreach (var entry in eventData.Context.ChangeTracker.Entries<User>())
{
    if (entry.State == EntityState.Modified)
    {
        _pendingLogs.Add((entry, new UserHistory
        {
            UserId = entry.Entity.Id,
            Operation = "UPDATE",
            OldName = entry.OriginalValues.GetValue<string?>(nameof(User.Name)),
            NewName = entry.CurrentValues.GetValue<string?>(nameof(User.Name)),
            ChangedAtUtc = DateTime.UtcNow
        }));
    }
}
```

Збереження записів аудиту в `SavedChanges` (коли для `INSERT` уже відомий згенерований `Id`):

```csharp
foreach (var (entry, history) in _pendingLogs)
{
    if (history.UserId == 0)
        history.UserId = entry.Entity.Id;

    eventData.Context.Set<UserHistory>().Add(history);
}
```

---

## Особливості роботи в SQLite

- **`IsTemporal()` недоступний у SQLite:** виклик `.IsTemporal()` при використанні провайдера `Microsoft.EntityFrameworkCore.Sqlite` не підтримується (метод розширення належить пакету `Microsoft.EntityFrameworkCore.SqlServer`).
- **Відновлення запису зі старим `Id`:** у SQL Server для вставки явного `Id` потрібна команда `SET IDENTITY_INSERT [dbo].[Users] ON`. У SQLite для стовпця `INTEGER PRIMARY KEY` ви можете просто вказати `Id = oldId` при `Add(...)` — SQLite дозволяє вставляти явні значення первинного ключа без додаткових команд.
- **Масові операції (`ExecuteUpdate` / `ExecuteDelete`):** `SaveChangesInterceptor` спрацьовує лише при роботі через `SaveChanges()`. Якщо у проєкті використовуються `ExecuteUpdate` або `ExecuteDelete` в обхід `ChangeTracker`, для гарантованого аудиту в SQLite слід використовувати **SQL-тригери**.

---

## Підсумок та підводні камені

- **Захист від нескінченної рекурсії в інтерсепторі:** якщо ви викликаєте `context.SaveChanges()` всередині `SavedChanges(...)` для запису таблиці аудиту, обов'язково використовуйте прапорець-запобіжник (`_isSavingAudit`), інакше інтерсептор викличе сам себе по колу.
- **Часова зона:** у системах аудиту та Temporal Tables завжди зберігайте мітки часу в **UTC** (`DateTime.UtcNow`), щоб уникнути плутанини при переході на літній/зимовий час.
- **Обмеження навігації:** у SQL Server методи `Temporal*` працюють у режимі `AsNoTracking` — отримані історичні об'єкти не відстежуються контекстом.
