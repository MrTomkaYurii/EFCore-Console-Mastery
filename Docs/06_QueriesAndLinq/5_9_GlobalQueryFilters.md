# 5.9. Фільтри запитів рівня моделі (HasQueryFilter)

- **Глава:** 06. Запити та LINQ to Entities
- **Оригінал на Metanit:** [Фильтры запросов уровня модели](https://metanit.com/sharp/efcore/5.9.php)
- **Код прикладу:** [Lesson_5_9_GlobalQueryFilters.cs](../../src/06_QueriesAndLinq/Lesson_5_9_GlobalQueryFilters.cs)

## Мета уроку

Навчитися налаштовувати глобальні фільтри запитів на рівні моделі (`HasQueryFilter`) в `OnModelCreating` (типові сценарії: Soft Delete та Multi-Tenancy) і за потреби вимикати їх через `IgnoreQueryFilters()`.

## Теоретичний матеріал

### Що таке глобальний фільтр запитів
**Model-level query filter** — це LINQ-предикат, який задається в метаданих моделі (`OnModelCreating`) через `modelBuilder.Entity<T>().HasQueryFilter(...)`.

Після налаштування EF Core **автоматично** додає цю умову в `WHERE` до **кожного** LINQ-запиту, що звертається до цієї сутності:
- звичайні `Where`, `ToList`, `FirstOrDefault`;
- агрегатні запити (`Count`, `Min`, `Max`, `Any`);
- завантаження через `Include` (у тому числі фільтровані навігаційні колекції);
- `ExecuteUpdate` та `ExecuteDelete`.

Фільтр може посилатися на властивості самого екземпляра `DbContext` (наприклад, `CurrentRoleId` або `TenantId`). EF Core коректно параметризує такі значення для кожного окремого екземпляра контексту!

### Відключення фільтра (`IgnoreQueryFilters`)
Якщо в конкретному запиті (наприклад, для адміністратора) потрібно побачити всі записи, включно з відфільтрованими, викликають `.IgnoreQueryFilters()`.

## Ключові фрагменти коду

```csharp
// Налаштування в DbContext.OnModelCreating
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>()
        .HasQueryFilter(u => u.Age > 17 && u.RoleId == CurrentRoleId);
}

// Запит із автоматично застосованим фільтром
int minFilteredAge = db.Users.Min(u => u.Age);

// Запит із вимкненим глобальним фільтром
int minAllAge = db.Users.IgnoreQueryFilters().Min(u => u.Age);
```

## Особливості роботи в SQLite

- У SQLite умова з `HasQueryFilter` транслюється у звичайний параметризований `WHERE` (наприклад, `WHERE "u"."Age" > 17 AND "u"."RoleId" = @__ef_filter__CurrentRoleId_0`), тому план запиту кешується так само ефективно, як і в SQL Server.

## Підсумок та підводні камені

- Для однієї сутності можна задати лише **один** `HasQueryFilter` (у класичному API виклик другого `HasQueryFilter` перезаписує перший — комбінуйте умови через `&&`).
- **Обережно зі зв'язками (Required navigation + Query Filter):** якщо у `Order` є обов'язковий зв'язок з `User`, а на `User` накладено `HasQueryFilter` (наприклад, Soft Delete `IsDeleted == false`), то запити до `Order` без фільтра можуть несподівано втрачати замовлення видалених користувачів через `INNER JOIN`. У таких випадках варто або дублювати фільтр на залежній сутності, або робити зовнішній ключ nullable.
- `Find()` / `FindAsync()` **не ігнорують** глобальний фільтр: якщо запис у БД не відповідає фільтру (і його немає в кеші), `Find` поверне `null`.
