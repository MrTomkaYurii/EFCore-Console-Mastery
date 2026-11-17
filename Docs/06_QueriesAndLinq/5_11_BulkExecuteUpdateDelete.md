# 5.11. Масове оновлення та видалення. ExecuteUpdate і ExecuteDelete

- **Глава:** 06. Запити та LINQ to Entities
- **Оригінал на Metanit:** [Массовое обновление и удаление. ExecuteUpdate и ExecuteDelete](https://metanit.com/sharp/efcore/5.11.php)
- **Код прикладу:** [Lesson_5_11_BulkExecuteUpdateDelete.cs](../../src/06_QueriesAndLinq/Lesson_5_11_BulkExecuteUpdateDelete.cs)

## Мета уроку

Навчитися виконувати масове оновлення та видалення записів одним SQL-запитом без попереднього завантаження об'єктів у пам'ять за допомогою `ExecuteUpdate` / `ExecuteUpdateAsync` та `ExecuteDelete` / `ExecuteDeleteAsync`.

## Теоретичний матеріал

### Навіщо потрібні `ExecuteUpdate` та `ExecuteDelete`
Класичний підхід в EF Core (`завантажити -> змінити / Remove -> SaveChanges`) вимагає зайвого мережевого/дискового обміну та витрат пам'яті, коли потрібно оновити або видалити десятки чи тисячі рядків за певною умовою.

Починаючи з EF Core 7.0, для `IQueryable<T>` доступні методи масових операцій:
- **`ExecuteUpdate` / `ExecuteUpdateAsync`** — формує один SQL-запит `UPDATE ... SET ... WHERE ...` і повертає кількість оновлених рядків (`int`). За допомогою ланцюжка `.SetProperty(selector, valueExpression)` можна оновити кілька стовпців за раз.
- **`ExecuteDelete` / `ExecuteDeleteAsync`** — формує один SQL-запит `DELETE FROM ... WHERE ...` і повертає кількість видалених рядків (`int`).

### Порівняння підходів

| Критерій | `SaveChanges()` | `ExecuteUpdate` / `ExecuteDelete` |
| :--- | :--- | :--- |
| Попереднє завантаження сутностей | Потрібне (`SELECT` + `ChangeTracker`) | Не потрібне (одразу `UPDATE` / `DELETE`) |
| Синхронізація з `ChangeTracker` | Автоматична | **Відсутня** (обходить `ChangeTracker`) |
| Швидкодія на масових даних | Нижча | Значно вища (1 SQL-запит) |

## Ключові фрагменти коду

```csharp
// Масове оновлення кількох властивостей за умовою Where
int updated = db.Users
    .Where(u => u.Name == "Tom")
    .ExecuteUpdate(s => s
        .SetProperty(u => u.Age, u => u.Age + 10)
        .SetProperty(u => u.Name, _ => "Tomas"));

// Масове видалення за умовою Where
int deleted = db.Users
    .Where(u => u.Name == "Bob")
    .ExecuteDelete();
```

Згенерований SQL для `ExecuteUpdate` у SQLite:

```sql
UPDATE "Users" AS "u"
SET "Name" = 'Tomas',
    "Age" = "u"."Age" + 10
WHERE "u"."Name" = 'Tom'
```

## Особливості роботи в SQLite

- У SQL Server EF Core генерує синтаксис `UPDATE [u] SET ... FROM [Users] AS [u]`, тоді як у SQLite — `UPDATE "Users" AS "u" SET ...` та `DELETE FROM "Users" AS "u" WHERE ...`.
- Обидва методи виконуються негайно в межах поточної транзакції (або створюють неявну транзакцію, якщо активної немає) без виклику `SaveChanges()`.

## Підсумок та підводні камені

- **Головний підводний камінь:** `ExecuteUpdate` і `ExecuteDelete` **не оновлюють `ChangeTracker`**. Якщо сутність уже була завантажена в поточний `DbContext` раніше, її властивості в пам'яті залишаться старими! Щоб отримати актуальні значення, викличте `db.Entry(entity).Reload()` або використовуйте новий `DbContext`.
- Ці методи працюють лише з **однією таблицею** за виклик (не можна в одному `ExecuteUpdate` оновити поля і `User`, і пов'язаної `Company`).
- При `ExecuteDelete` потрібно враховувати обмеження зовнішніх ключів (`FOREIGN KEY`) у БД: за відсутності каскадного видалення спочатку видаляйте залежні записи.
