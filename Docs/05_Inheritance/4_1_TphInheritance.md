# 4.1. Підхід TPH - Table Per Hierarchy

- **Глава:** 05. Спадкування
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/4.1.php](https://metanit.com/sharp/efcore/4.1.php)
- **Код прикладу:** [Lesson_4_1_TphInheritance.cs](../../src/05_Inheritance/Lesson_4_1_TphInheritance.cs)

## Мета уроку

Зрозуміти, як Entity Framework Core за замовчуванням відображає ієрархію успадкування класів у реляційну базу даних за стратегією **TPH (Table Per Hierarchy)**, як працює службовий стовпець-дискримінатор (`Discriminator`) та який SQL генерується під час поліморфних і типізованих запитів.

## Теоретичний матеріал

### Що таке TPH (Table Per Hierarchy)

**TPH (Таблиця на одну ієрархію класів)** — це стратегія відображення спадкування, яку EF Core використовує **за замовчуванням**. За цим підходом для всіх класів однієї ієрархії створюється **одна спільна таблиця** у базі даних.

Щоб EF Core міг розрізняти, до якого саме класу C# належить конкретний рядок таблиці, у ній автоматично створюється додатковий службовий стовпець **`Discriminator`**.

### Включення ієрархії в модель

Щоб усі типи з ієрархії (`User`, `Employee`, `Manager`) потрапили до моделі даних та були доступні для запитів, у `DbContext` визначають відповідні набори `DbSet<T>`:

- `DbSet<User> Users`
- `DbSet<Employee> Employees`
- `DbSet<Manager> Managers`

У результаті міграції або `EnsureCreated()` у БД створюється одна таблиця `Users`, що містить:
- стовпці базового класу (`Id`, `Name`);
- стовпець `Discriminator` (`TEXT NOT NULL` у SQLite);
- стовпці всіх похідних класів (`Salary`, `Departament`), причому **властивості похідних класів стають nullable** на рівні БД (адже рядок для `User` або `Manager` не має `Salary`).

### Властивість `Discriminator` у моделі

За замовчуванням `Discriminator` є тіньовою властивістю (shadow property) — її немає в класі `User`, але EF Core керує нею автоматично. За потреби ми можемо явно додати `public string? Discriminator { get; set; }` до базового класу `User`, щоб читати її значення в коді.

> Важливо: на рівні моделі дискримінатор налаштований як **лише для читання** (read-only після створення) — EF Core сам підставляє ім'я типу при збереженні.

### Явне налаштування `UseTphMappingStrategy()`

Хоча TPH діє за замовчуванням, починаючи з EF Core 7.0 стратегію можна вказати явно в `OnModelCreating`:

```csharp
modelBuilder.Entity<User>().UseTphMappingStrategy();
```

Це корисно для читабельності коду або при явному перевизначенні конфігурації базового типу.

### Порівняння запитів у TPH

| Запит у C# | Що відбувається в SQL | Особливість |
| :--- | :--- | :--- |
| `db.Users.ToList()` | `SELECT ... FROM "Users"` | Читає всі рядки без `JOIN` |
| `db.Employees.ToList()` | `SELECT ... FROM "Users" WHERE "Discriminator" = 'Employee'` | Простий фільтр по одному стовпцю |
| `db.Managers.ToList()` | `SELECT ... FROM "Users" WHERE "Discriminator" = 'Manager'` | Простий фільтр по одному стовпцю |

## Ключові фрагменти коду

Ієрархія моделей з опціональною властивістю `Discriminator`:

```csharp
public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Discriminator { get; set; }
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

Конфігурація контексту:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>().UseTphMappingStrategy();
}
```

Перегляд реальної схеми SQLite та згенерованого SQL у прикладі:
- `sqlite_master` показує одну таблицю `"Users"` зі стовпцями `"Id"`, `"Name"`, `"Discriminator"`, `"Salary"`, `"Departament"`.
- `db.Employees.ToQueryString()` демонструє умову `WHERE "u"."Discriminator" = 'Employee'`.

## Особливості роботи в SQLite

- У статті Metanit згадується тип `nvarchar` (термінологія SQL Server), проте в прикладі для SQLite стовпці рядків створюються з типом `TEXT`.
- `Salary` (тип `int` у C#) у класі `Employee` не є nullable, але в спільній таблиці `Users` у SQLite стовпець `"Salary" INTEGER` **не має обмеження `NOT NULL`**, щоб дозволити збереження рядків `User` та `Manager`.
- Стовпець `"Discriminator" TEXT NOT NULL` у SQLite за замовчуванням має обмеження `NOT NULL`.

## Підсумок та підводні камені

- **Плюси TPH:** максимальна продуктивність запитів (жодних `JOIN` або `UNION ALL`), найпростіша схема з однією таблицею.
- **Мінуси TPH:** усі стовпці похідних класів мусять бути nullable на рівні БД (неможливо поставити `NOT NULL` у БД для специфічного поля похідного класу без додаткових `CHECK`-обмежень); при великій кількості похідних класів таблиця стає «широкою» з багатьма `NULL`.
- **Підводний камінь:** не намагайтеся вручну змінювати `Discriminator` при оновленні сутності — EF Core сприймає тип екземпляра як незмінний після створення.
