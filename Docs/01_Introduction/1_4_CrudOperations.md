# 1.4. Основні операції з даними. CRUD

- **Глава:** 01. Вступ до Entity Framework Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/1.4.php](https://metanit.com/sharp/efcore/1.4.php)
- **Код прикладу:** [`Lesson_1_4_CrudOperations.cs`](../../src/01_Introduction/Lesson_1_4_CrudOperations.cs)

## Мета уроку

Опанувати базові операції з даними **CRUD** (*Create, Read, Update, Delete*) в EF Core, зрозуміти різницю між відстежуваним (*Connected*) та від'єднаним (*Disconnected*) оновленням сутностей, а також навчитися використовувати пакетні та асинхронні методи.

## Теоретичний матеріал

### Операції CRUD у `DbSet<T>` та `DbContext`

| Операція | Метод EF Core | Стан сутності (`EntityState`) | Згенерований SQL |
| --- | --- | --- | --- |
| **Create** | `Add()`, `AddRange()` | `Added` | `INSERT INTO ...` |
| **Read** | `ToList()`, `FirstOrDefault()` | `Unchanged` | `SELECT ... FROM ...` |
| **Update (Tracked)** | Зміна властивостей + `SaveChanges()` | `Modified` (автоматично) | `UPDATE ... SET ...` |
| **Update (Disconnected)** | `Update()`, `UpdateRange()` | `Modified` (примусово) | `UPDATE ... SET ...` |
| **Delete** | `Remove()`, `RemoveRange()` | `Deleted` | `DELETE FROM ...` |

### Відстежуваний vs Від'єднаний сценарій оновлення
- **У межах одного `DbContext`:** контекст уже відстежує (*Change Tracker*) завантажену сутність. Коли ви змінюєте `user.Name = "Bob";`, EF Core самостійно фіксує зміну конкретної властивості й формує `UPDATE` лише для змінених стовпців при виклику `SaveChanges()`.
- **У різних екземплярах `DbContext` (від'єднаний сценарій):** якщо об'єкт отримано в одному `using (var db1 = ...)`, а змінюється в іншому `using (var db2 = ...)`, новий контекст `db2` нічого не знає про цей об'єкт. Без виклику `db2.Users.Update(user)` метод `SaveChanges()` не виконає жодного SQL-запиту.

### Асинхронний API
Для неблокуючої роботи з БД в EF Core існують асинхронні методи:
- `AddAsync()` / `AddRangeAsync()` (використовуються переважно тоді, коли генератор ключів звертається до БД, наприклад `HiLo`; в інших випадках достатньо синхронного `Add()`).
- `ToListAsync()`, `FirstOrDefaultAsync()` (з простору імен `Microsoft.EntityFrameworkCore`).
- `SaveChangesAsync()`.

## Ключові фрагменти коду

Від'єднане оновлення через `Update()`:

```csharp
User? detachedUser;
using (var db1 = new CrudContext(connectionString))
{
    detachedUser = db1.Users.FirstOrDefault(u => u.Name == "Alice");
}

if (detachedUser is not null)
{
    detachedUser.Name = "Alice Updated";
    using var db2 = new CrudContext(connectionString);
    db2.Users.Update(detachedUser); // Позначає всі властивості як Modified
    db2.SaveChanges();
}
```

Асинхронні операції:

```csharp
await db.Users.AddRangeAsync(sam, mike);
await db.SaveChangesAsync();

var found = await db.Users.FirstOrDefaultAsync(u => u.Name == "Sam");
var allAsync = await db.Users.ToListAsync();
```

## Особливості роботи в SQLite

- У SQLite кожен виклик `SaveChanges()` за замовчуванням виконується в межах транзакції, що гарантує атомарність пакетних операцій (`AddRange`, `RemoveRange`).
- На відміну від серверних СУБД (SQL Server, PostgreSQL), SQLite блокує файл БД на запис на рівні файлу: паралельні запити на запис з кількох потоків/процесів можуть викликати `SQLite Error 5: 'database is locked'` без налаштування `BusyTimeout` або `WAL`-режиму.

## Підсумок та підводні камені

- Метод `DbSet.Update(entity)` позначає **всі** властивості сутності як змінені, що може перезаписати поля, які ви не планували змінювати. Якщо об'єкт уже відстежується контекстом, викликати `Update()` не потрібно.
- Для ефективного масового видалення або оновлення без завантаження сутностей у пам'ять у сучасному EF Core (починаючи з EF Core 7+) краще використовувати `ExecuteUpdate` / `ExecuteDelete` (розглядається в наступних главах).
- Пам'ятайте: `Add`, `Remove`, `Update` лише змінюють стан у пам'яті — без `SaveChanges()` / `SaveChangesAsync()` база даних не зміниться.
