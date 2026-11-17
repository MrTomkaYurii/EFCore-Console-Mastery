# 5.3. З'єднання та групування таблиць (Join, GroupBy)

- **Глава:** 06. Запити та LINQ to Entities
- **Оригінал на Metanit:** [Соединение и группировка таблиц](https://metanit.com/sharp/efcore/5.3.php)
- **Код прикладу:** [Lesson_5_3_JoinAndGroupBy.cs](../../src/06_QueriesAndLinq/Lesson_5_3_JoinAndGroupBy.cs)

## Мета уроку

Навчитися явно об'єднувати дві та більше таблиць за допомогою методу `Join` і оператора `join ... equals`, а також групувати дані на стороні БД за допомогою `GroupBy` (`group ... by ... into`).

## Теоретичний матеріал

### Явне з'єднання таблиць (`Join`)
Хоча за наявності навігаційних властивостей EF Core сам будує `JOIN` через `Include` або `Select`, метод `Join` дозволяє з'єднати будь-які два набори за довільними ключами (навіть якщо між ними немає зовнішнього ключа в моделі).

Метод `Join` приймає 4 аргументи:
1. Другий набір даних (`db.Companies`).
2. Селектор ключа з першого набору (`u => u.CompanyId`).
3. Селектор ключа з другого набору (`c => c.Id`).
4. Функція проекції результату `(u, c) => new { ... }`.

Для з'єднання трьох і більше таблиць особливо читабельним є **синтаксис запитів LINQ** (`join ... on ... equals ...`).

### Групування (`GroupBy`)
`GroupBy` групує рядки за ключем (`g.Key`) та дозволяє застосовувати агрегатні функції до кожної групи (`g.Count()`, `g.Average(...)`, `g.Sum(...)`, `g.Min(...)`, `g.Max(...)`). На рівні SQL це транслюється в `GROUP BY`.

## Ключові фрагменти коду

```csharp
// З'єднання трьох таблиць (Query Syntax)
var users = from user in db.Users
            join company in db.Companies on user.CompanyId equals company.Id
            join country in db.Countries on company.CountryId equals country.Id
            select new
            {
                user.Name,
                Company = company.Name,
                Country = country.Name,
                user.Age
            };

// Групування з агрегацією (Method Syntax)
var groups = db.Users
    .GroupBy(u => u.Company!.Name)
    .Select(g => new
    {
        CompanyName = g.Key,
        Count = g.Count(),
        AvgAge = g.Average(u => u.Age)
    });
```

## Особливості роботи в SQLite

- У SQLite `AVG(integer_column)` повертає число з рухомою комою (`REAL`), і EF Core автоматично приводить тип до `double`.
- `Join` у LINQ завжди генерує `INNER JOIN`. Для `LEFT JOIN` у LINQ to Entities використовують `GroupJoin` + `SelectMany` + `DefaultIfEmpty()` (або навігаційну властивість з nullable зовнішнім ключем у `Select`).

## Підсумок та підводні камені

- У `GroupBy` у SQL-запит можна спроектувати лише ключ групування (`g.Key`) та агрегати по групі (`Count`, `Sum`, `Average`, `Min`, `Max`). Спроба повернути саму колекцію елементів групи без агрегації у більшості випадків не транслюється в один SQL `GROUP BY` або потребує окремих запитів.
- У операторі `join ... on a equals b` завжди використовується ключове слово `equals`, а не `==`.
