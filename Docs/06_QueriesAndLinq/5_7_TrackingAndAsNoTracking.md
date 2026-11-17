# 5.7. Відстеження об'єктів та AsNoTracking

- **Глава:** 06. Запити та LINQ to Entities
- **Оригінал на Metanit:** [Отслеживание объектов и AsNoTracking](https://metanit.com/sharp/efcore/5.7.php)
- **Код прикладу:** [Lesson_5_7_TrackingAndAsNoTracking.cs](../../src/06_QueriesAndLinq/Lesson_5_7_TrackingAndAsNoTracking.cs)

## Мета уроку

Зрозуміти механізм відстеження змін (`ChangeTracker`) в EF Core, навчитися вимикати його для запитів «лише для читання» через `AsNoTracking()` або глобально через `ChangeTracker.QueryTrackingBehavior`.

## Теоретичний матеріал

### Відстежувані запити (Tracking)
За замовчуванням усі запити, які повертають сутності моделі (наприклад, `User`), є **відстежуваними**:
- EF Core зберігає копію початкових значень (snapshot) у `ChangeTracker`.
- Коли ви змінюєте властивість об'єкта (`user.Age = 18`) і викликаєте `db.SaveChanges()`, EF Core автоматично генерує `UPDATE` лише для змінених стовпців.
- Це витрачає додаткову пам'ять і процесорний час на побудову знімків і перевірку зв'язків.

### Невідстежувані запити (`AsNoTracking`)
Коли дані потрібні **лише для відображення** (read-only), відстеження можна вимкнути:
1. **Для конкретного запиту:** `db.Users.AsNoTracking().ToList()`.
2. **Для всього екземпляра контексту:** `db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;` (або в конструкторі контексту / `OnConfiguring` через `UseQueryTrackingBehavior`).
3. **Повторне увімкнення для окремого запиту:** `db.Users.AsTracking().FirstOrDefault()`.

## Ключові фрагменти коду

```csharp
// 1. Невідстежуваний запит — зміни не потраплять у БД при SaveChanges()
var user = db.Users.AsNoTracking().FirstOrDefault();
if (user is not null)
{
    user.Age = 99;
    db.SaveChanges(); // Оновить 0 рядків!
}

// 2. Вимкнення відстеження на рівні всього контексту
db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

// 3. Точкове повернення відстеження
var trackedUser = db.Users.AsTracking().FirstOrDefault();
```

## Особливості роботи в SQLite

- Сама SQL-команда `SELECT` у SQLite не змінюється від використання `AsNoTracking()`. Різниця полягає виключно в роботі EF Core у пам'яті застосунку (`ChangeTracker` не реєструє отримані об'єкти).
- Використання `AsNoTracking()` у поєднанні з SQLite дає помітний приріст швидкості при читанні великих списків (тисячі рядків).

## Підсумок та підводні камені

- Якщо ви завантажили сутність з `AsNoTracking()`, а потім усе ж хочете її оновити, потрібно явно прикріпити її до контексту через `db.Users.Update(user)` або `db.Attach(user)` перед `SaveChanges()`.
- Запити з проекцією `Select(u => new UserDto { ... })` і так не відстежуються, навіть без `AsNoTracking()`.
- Також існує метод `AsNoTrackingWithIdentityResolution()`, який не реєструє сутності в `ChangeTracker`, але гарантує, що однакові сутності в межах одного запиту будуть представлені одним екземпляром у пам'яті.
