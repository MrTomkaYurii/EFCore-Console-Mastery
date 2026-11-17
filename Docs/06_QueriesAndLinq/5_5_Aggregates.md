# 5.5. Агрегатні операції (Any, All, Count, Sum, Min, Max, Average)

- **Глава:** 06. Запити та LINQ to Entities
- **Оригінал на Metanit:** [Агрегатные операции](https://metanit.com/sharp/efcore/5.5.php)
- **Код прикладу:** [Lesson_5_5_Aggregates.cs](../../src/06_QueriesAndLinq/Lesson_5_5_Aggregates.cs)

## Мета уроку

Навчитися обчислювати скалярні підсумкові значення безпосередньо в SQLite (`Any`, `All`, `Count`, `Sum`, `Min`, `Max`, `Average`), не завантажуючи рядки таблиці у пам'ять застосунку.

## Теоретичний матеріал

Агрегатні методи LINQ виконуються **негайно** (Immediate Execution) і повертають одне скалярне значення (`bool`, `int`, `double` тощо).

| Метод | Призначення | SQL-еквівалент у SQLite |
| :--- | :--- | :--- |
| `Any(predicate)` | Перевіряє, чи існує хоча б один елемент | `SELECT EXISTS (SELECT 1 ... WHERE ...)` |
| `All(predicate)` | Перевіряє, чи всі елементи задовольняють умову | `SELECT NOT EXISTS (SELECT 1 ... WHERE NOT (...))` |
| `Count()` / `Count(p)` | Підраховує кількість записів | `SELECT COUNT(*)` |
| `Sum(selector)` | Обчислює суму значень | `SELECT COALESCE(SUM(...), 0)` |
| `Min(selector)` | Знаходить мінімальне значення | `SELECT MIN(...)` |
| `Max(selector)` | Знаходить максимальне значення | `SELECT MAX(...)` |
| `Average(selector)` | Обчислює середнє арифметичне | `SELECT AVG(CAST(... AS REAL))` |

## Ключові фрагменти коду

```csharp
bool hasGoogleUsers = db.Users.Any(u => u.Company!.Name == "Google");
bool allMicrosoft = db.Users.All(u => u.Company!.Name == "Microsoft");

int totalUsers = db.Users.Count();
int minAge = db.Users.Min(u => u.Age);
int maxAge = db.Users.Max(u => u.Age);

double avgMicrosoftAge = db.Users
    .Where(u => u.Company!.Name == "Microsoft")
    .Average(u => u.Age);

int sumMicrosoft = db.Users
    .Where(u => u.Company!.Name == "Microsoft")
    .Sum(u => u.Age);
```

## Особливості роботи в SQLite

- У SQLite `SUM()` для порожньої вибірки повертає `NULL`, тому EF Core огортає його в `COALESCE(SUM(...), 0)` для не-nullable числових типів.
- Для `Average()` над цілочисельним стовпцем EF Core додає `CAST("u"."Age" AS REAL)`, щоб уникнути цілочисельного ділення.
- Для `decimal` у SQLite агрегати (`Sum`, `Average`, `Min`, `Max`) історично мали обмеження (оскільки SQLite не має нативного типу `DECIMAL` і зберігає його як `TEXT`). Для `int` та `double` усе працює нативно на боці БД.

## Підсумок та підводні камені

- **Порожня вибірка та `Min`/`Max`/`Average`:** виклик `Min(u => u.Age)` на порожній таблиці для не-nullable `int` викине `InvalidOperationException` (бо SQL поверне `NULL`). Безпечніше проєктувати в nullable (`Min(u => (int?)u.Age) ?? 0`).
- Для перевірки наявності записів завжди використовуйте `Any()`, а не `Count() > 0` — `EXISTS` зупиняється на першому знайденому рядку й працює значно швидше.
