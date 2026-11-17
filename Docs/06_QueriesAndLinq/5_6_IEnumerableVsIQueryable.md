# 5.6. IEnumerable та IQueryable

- **Глава:** 06. Запити та LINQ to Entities
- **Оригінал на Metanit:** [IEnumerable и IQueryable](https://metanit.com/sharp/efcore/5.6.php)
- **Код прикладу:** [Lesson_5_6_IEnumerableVsIQueryable.cs](../../src/06_QueriesAndLinq/Lesson_5_6_IEnumerableVsIQueryable.cs)

## Мета уроку

Зрозуміти фундаментальну різницю між інтерфейсами `IEnumerable<T>` та `IQueryable<T>` у контексті EF Core, щоб уникати випадкового вивантаження всієї таблиці в пам'ять перед фільтрацією.

## Теоретичний матеріал

Хоча `IQueryable<T>` успадковується від `IEnumerable<T>`, їхня поведінка в LINQ принципово різна:

| Характеристика | `IEnumerable<T>` | `IQueryable<T>` |
| :--- | :--- | :--- |
| **Простір імен** | `System.Collections.Generic` | `System.Linq` |
| **Тип предиката у `Where`** | `Func<T, bool>` (делегат) | `Expression<Func<T, bool>>` (дерево виразів) |
| **Де виконується фільтрація** | У пам'яті застосунку (LINQ to Objects) | На сервері / у рушії БД (SQL `WHERE`) |
| **Згенерований SQL** | `SELECT "Id", "Name" FROM "Users"` (усі рядки!) | `SELECT "Id", "Name" FROM "Users" WHERE "Id" < 10` |
| **Динамічна побудова запиту** | Неоптимально (кожен крок фільтрує вже в пам'яті) | Оптимально: умови об'єднуються в один `WHERE ... AND ...` |

### Чому так відбувається?
Коли ви присвоюєте `db.Users` змінній типу `IEnumerable<User>`, подальший виклик `.Where(...)` викликає метод розширення `Enumerable.Where`, який не знає про базу даних і просто перебирає рядки, завантажені з SQLite.

Навпаки, `IQueryable<User>` використовує `Queryable.Where`, який добудовує дерево виразів. Лише при виклику `ToList()` EF Core оптимізує й перекладає весь ланцюжок в один SQL-запит.

## Ключові фрагменти коду

```csharp
// Погано для великих таблиць: фільтрація на стороні клієнта (у пам'яті)
IEnumerable<User> userIEnum = db.Users;
var clientFiltered = userIEnum.Where(u => u.Id < 10).ToList();

// Добре: фільтрація на стороні SQLite (SQL WHERE)
IQueryable<User> userIQuer = db.Users;
userIQuer = userIQuer.Where(u => u.Id < 10);
userIQuer = userIQuer.Where(u => u.Name == "Tom");
var serverFiltered = userIQuer.ToList();
```

## Особливості роботи в SQLite

- На локальному файлі SQLite різниця між завантаженням 100 рядків і 1 рядка може здаватися непомітною, але на таблиці зі 100 000+ записів `IEnumerable<T>` вивантажить усі 100 000 рядків у RAM перед фільтрацією, тоді як `IQueryable<T>` прочитає лише потрібні рядки (особливо за наявності індексу).

## Підсумок та підводні камені

- **Поширена помилка:** написати метод-репозиторій, який повертає `IEnumerable<User>` замість `IQueryable<User>`, а потім застосовувати `.Where(...)` або `.Skip().Take()` у контролері/сервісі — уся пагінація й фільтрація відбудеться в пам'яті!
- Якщо потрібно виконати C#-метод, який не перекладається в SQL, спочатку відфільтруйте максимум даних через `IQueryable`, викличте `.AsEnumerable()` або `.ToList()`, і лише потім застосовуйте складну C#-логіку.
