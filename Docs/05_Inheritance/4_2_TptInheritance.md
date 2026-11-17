# 4.2. Підхід TPT - Table Per Type

- **Глава:** 05. Спадкування
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/4.2.php](https://metanit.com/sharp/efcore/4.2.php)
- **Код прикладу:** [Lesson_4_2_TptInheritance.cs](../../src/05_Inheritance/Lesson_4_2_TptInheritance.cs)

## Мета уроку

Навчитися налаштовувати стратегію **TPT (Table Per Type)** за допомогою атрибута `[Table]`, Fluent API `ToTable()` та методу `UseTptMappingStrategy()`, а також побачити на реальному SQL (`ToQueryString()`) і схемі `sqlite_master`, як EF Core об'єднує нормалізовані таблиці через `INNER JOIN` та `LEFT JOIN`.

## Теоретичний матеріал

### Що таке TPT (Table Per Type)

**TPT (Таблиця на кожен тип)** — це стратегія відображення спадкування, за якої **для кожного класу ієрархії створюється окрема таблиця** у базі даних:
- базова таблиця `Users` зберігає спільні властивості (`Id`, `Name`);
- похідна таблиця `Employees` зберігає лише `Id` та `Salary`;
- похідна таблиця `Managers` зберігає лише `Id` та `Departament`.

Зв'язок між базовою та похідними таблицями будується як **один-до-одного** через спільний первинний ключ `Id`, який у похідних таблицях одночасно є зовнішнім ключем (`FOREIGN KEY ("Id") REFERENCES "Users"("Id") ON DELETE CASCADE`).

### Три способи налаштування TPT

| Спосіб | Приклад | Версія EF Core |
| :--- | :--- | :--- |
| Атрибути Data Annotations | `[Table("Employees")]` на похідному класі | Усі версії |
| Fluent API `ToTable()` | `modelBuilder.Entity<Employee>().ToTable("Employees");` | Усі версії |
| Fluent API `UseTptMappingStrategy()` | `modelBuilder.Entity<User>().UseTptMappingStrategy();` | Починаючи з EF Core 7.0 |

### Як виглядають SQL-запити в TPT

На відміну від TPH, у TPT немає стовпця `Discriminator`. Натомість EF Core визначає тип за наявністю рядка у відповідній дочірній таблиці:
- **`db.Users.ToList()` (поліморфний запит):** генерує `LEFT JOIN "Employees"` та `LEFT JOIN "Managers"` з конструкцією `CASE WHEN ...` для визначення конкретного типу.
- **`db.Employees.ToList()` (запит конкретного типу):** генерує `INNER JOIN "Users"` для отримання успадкованих властивостей (`Name`) разом із `Salary`.

## Ключові фрагменти коду

Налаштування через атрибут `[Table]` та явне оголошення стратегії `UseTptMappingStrategy()`:

```csharp
public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

[Table("Employees")]
public class Employee : User
{
    public int Salary { get; set; }
}

[Table("Managers")]
public class Manager : User
{
    public string? Departament { get; set; }
}
```

У `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>().UseTptMappingStrategy();
}
```

При додаванні `Employee` EF Core виконує два `INSERT`: спочатку в `"Users"` (отримує згенерований `Id`), а потім — у `"Employees"` з тим самим `Id`.

## Особливості роботи в SQLite

- У SQLite для таблиці `Users` створюється `PRIMARY KEY("Id" AUTOINCREMENT)`, а для `Employees` та `Managers` — `CONSTRAINT "FK_Employees_Users_Id" FOREIGN KEY("Id") REFERENCES "Users"("Id") ON DELETE CASCADE`.
- Стовпець `"Salary"` у таблиці `"Employees"` тепер має обмеження `INTEGER NOT NULL`, оскільки в цій таблиці зберігаються лише працівники (на відміну від TPH, де він був nullable).
- Усі особливості `ToQueryString()` (`LEFT JOIN` для `Users`, `INNER JOIN` для `Employees`/`Managers`) у SQLite працюють ідентично до SQL Server.

## Підсумок та підводні камені

- **Плюси TPT:** чиста нормалізована схема БД без дублювання стовпців і зайвих `NULL`; кожен похідний клас може мати суворі обмеження `NOT NULL`.
- **Мінуси TPT:** **найнижча продуктивність серед трьох стратегій** на складних ієрархіях, адже кожен поліморфний запит вимагає `LEFT JOIN` до всіх таблиць-нащадків, а запит конкретного типу — `INNER JOIN` до базової таблиці (або кількох, якщо ієрархія багаторівнева).
- **Підводний камінь:** якщо у вас багато похідних класів (наприклад, 10–15), поліморфні запити до базового `DbSet` можуть генерувати дуже важкі SQL-запити з десятками `LEFT JOIN`. У таких випадках варто розглянути TPH або TPC.
