# 5.4. Операції з множинами: Union, Intersect, Except

- **Глава:** 06. Запити та LINQ to Entities
- **Оригінал на Metanit:** [Операции с множествами](https://metanit.com/sharp/efcore/5.4.php)
- **Код прикладу:** [Lesson_5_4_SetOperations.cs](../../src/06_QueriesAndLinq/Lesson_5_4_SetOperations.cs)

## Мета уроку

Навчитися комбінувати результати двох однорідних вибірок безпосередньо на стороні бази даних за допомогою операцій над множинами: об'єднання (`Union`), перетину (`Intersect`) та різниці (`Except`).

## Теоретичний матеріал

Усі три методи працюють лише з **однорідними вибірками** — тобто обидва запити повинні повертати однаковий тип і однаковий набір стовпців.

| Метод LINQ | SQL-оператор | Результат |
| :--- | :--- | :--- |
| `Union` | `UNION` | Усі унікальні елементи з першої та другої вибірок (дублікати видаляються) |
| `Concat` | `UNION ALL` | Усі елементи з обох вибірок зі збереженням дублікатів |
| `Intersect` | `INTERSECT` | Лише ті елементи, які присутні в обох вибірках одночасно |
| `Except` | `EXCEPT` | Елементи першої вибірки, яких немає у другій вибірці |

Якщо потрібно об'єднати дані з **різних таблиць** (наприклад, `Users` та `Companies`), обидві вибірки попередньо приводять до спільного формату через `Select`:

```csharp
var allNames = db.Users.Select(u => new { u.Name })
    .Union(db.Companies.Select(c => new { c.Name }));
```

## Ключові фрагменти коду

```csharp
// Об'єднання (UNION)
var unionUsers = db.Users.Where(u => u.Age < 30)
    .Union(db.Users.Where(u => u.Name!.Contains("Tom")))
    .ToList();

// Перетин (INTERSECT)
var intersectUsers = db.Users.Where(u => u.Age > 30)
    .Intersect(db.Users.Where(u => u.Name!.Contains("Tom")))
    .ToList();

// Різниця (EXCEPT)
var exceptUsers = db.Users.Where(u => u.Age > 30)
    .Except(db.Users.Where(u => u.Name!.Contains("Tom")))
    .ToList();
```

## Особливості роботи в SQLite

- SQLite нативно підтримує всі три оператори: `UNION`, `INTERSECT` та `EXCEPT` (а також `UNION ALL` для `Concat`).
- Зверніть увагу на трансляцію `u.Name!.Contains("Tom")`: у SQLite EF Core перетворює це на `instr("u"."Name", 'Tom') > 0` (або `LIKE`). Функція `instr` у SQLite є чутливою до регістру.

## Підсумок та підводні камені

- Спроба об'єднати `db.Users` і `db.Companies` без попереднього `Select` не скомпілюється через невідповідність типів.
- Оскільки `Union`, `Intersect` та `Except` видаляють дублікати, СУБД виконує додаткове сортування або хешування. Якщо дублікати не заважають (або їх гарантовано немає), `Concat` (`UNION ALL`) працює швидше.
