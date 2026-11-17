# 4.3. Підхід TPC - Table Per Class

- **Глава:** 05. Спадкування
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/4.3.php](https://metanit.com/sharp/efcore/4.3.php)
- **Код прикладу:** [Lesson_4_3_TpcInheritance.cs](../../src/05_Inheritance/Lesson_4_3_TpcInheritance.cs)

## Мета уроку

Опанувати стратегію **TPC (Table Per Concrete Type / Table Per Class)**, що з'явилася в EF Core 7.0, зрозуміти принцип денормалізації таблиць без `JOIN` та особливості генерації унікальних первинних ключів у SQLite порівняно з SQL Server.

## Теоретичний матеріал

### Що таке TPC (Table Per Concrete Type)

**TPC (Таблиця на кожен конкретний тип)** створює окрему таблицю для кожного класу в ієрархії (`Users`, `Employees`, `Managers`), але на відміну від TPT **кожна таблиця містить усі стовпці, включно з успадкованими**:
- `Users`: `Id`, `Name`
- `Employees`: `Id`, `Name`, `Salary`
- `Managers`: `Id`, `Name`, `Departament`

Між таблицями **немає зовнішніх ключів**.

### Чому TPC часто швидший за TPT

| Тип запиту | TPT | TPC |
| :--- | :--- | :--- |
| Вибірка конкретного типу (`db.Employees`) | `INNER JOIN "Users"` | Прямий `SELECT` з `"Employees"` (без `JOIN`) |
| Поліморфний запит (`db.Users`) | `LEFT JOIN` усіх таблиць | `UNION ALL` між таблицями |

`UNION ALL` у більшості СУБД виконується значно швидше за множинні `LEFT JOIN`, а вибірка конкретного класу взагалі працює з однією таблицею без жодного `JOIN` чи фільтрації за дискримінатором.

### Вимога унікальності ідентифікаторів у TPC

EF Core вимагає, щоб **усі сутності в одній ієрархії мали унікальні значення первинного ключа `Id`**, навіть якщо вони зберігаються в різних таблицях. Наприклад, `Employee` не може мати такий самий `Id`, як `Manager`.

Оскільки в TPC немає єдиної базової таблиці для генерації автоінкременту, використовуються такі підходи:
1. **Явна генерація на боці клієнта** — наприклад, `string Id = Guid.NewGuid().ToString()` або `Guid Id`.
2. **Послідовності БД (`SEQUENCE`)** — стратегія за замовчуванням у SQL Server (створюється `UserSequence` з `DEFAULT (NEXT VALUE FOR [UserSequence])`).
3. **Різні діапазони `Identity`** (наприклад, `UseIdentityColumn(10000, 1)` для SQL Server) — ризиковано при розростанні даних.

## Ключові фрагменти коду

Моделі з `Guid`-ідентифікатором для повної сумісності з SQLite:

```csharp
public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? Name { get; set; }
}

public class Employee : User
{
    public int Salary { get; set; }
}

public class Manager : User
{
    public string? Departament { get; set; }
}
```

Налаштування стратегії TPC у `DbContext`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>().UseTpcMappingStrategy();
}
```

У `ToQueryString()`:
- `db.Users` генерує запит із `UNION ALL` між `"Users"`, `"Employees"` і `"Managers"`.
- `db.Employees` генерує простий `SELECT "e"."Id", "e"."Name", "e"."Salary" FROM "Employees" AS "e"`.

## Особливості роботи в SQLite

Це критично важлива відмінність від прикладів Metanit для SQL Server:
1. **SQLite не підтримує `SEQUENCE` (послідовності БД).** Через це стратегія за замовчуванням для `int Id` (яка в SQL Server створює `UserSequence`) у SQLite не працює автоматично. Тому для SQLite в прикладі використовується `public string Id { get; set; } = Guid.NewGuid().ToString();` (або тип `Guid`).
2. **Метод `UseIdentityColumn(seed, increment)`** із другої частини статті Metanit є специфічним для **SQL Server** і в провайдері SQLite відсутній.
3. Схема в `sqlite_master` показує три повністю незалежні таблиці без `FOREIGN KEY` та без `AUTOINCREMENT`.

## Підсумок та підводні камені

- **Плюси TPC:** оптимальні запити до конкретних типів (без `JOIN`), швидші поліморфні запити через `UNION ALL` порівняно з TPT, можливість задавати `NOT NULL` для властивостей похідних класів.
- **Мінуси TPC:** дублювання стовпців базового класу в кількох таблицях; складніше підтримувати зовнішні ключі з інших таблиць на базовий тип `User`.
- **Підводний камінь:** у SQLite не використовуйте стандартний автоінкремент `int Id` для TPC — це призведе до однакових `Id = 1` у різних таблицях і порушення контракту унікальності ключів в EF Core. Використовуйте `Guid` або `string` з `Guid.NewGuid()`.
