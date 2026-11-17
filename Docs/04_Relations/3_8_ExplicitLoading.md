# 3.8. Явне завантаження (Explicit loading)

- **Глава:** 04. Відносини між моделями
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/3.8.php](https://metanit.com/sharp/efcore/3.8.php)
- **Код прикладу:** [Lesson_3_8_ExplicitLoading.cs](../../src/04_Relations/Lesson_3_8_ExplicitLoading.cs)

## Мета уроку

Навчитися застосовувати стратегію **явного завантаження (Explicit Loading)** за допомогою методів `Load()`, `Entry().Collection().Load()` та `Entry().Reference().Load()`, а також комбінувати її з `.Query()` для фільтрації та агрегації без завантаження всієї колекції.

## Теоретичний матеріал

**Explicit Loading (явне завантаження)** — це підхід, коли пов'язані дані завантажуються в контекст вручну після того, як основна сутність уже отримана з бази даних.

### Способи явного завантаження

1. **Через `DbSet.Where(...).Load()`** — завантажує відфільтровані записи в локальний кеш контексту, а EF Core автоматично прив'язує їх до навігаційних властивостей уже завантажених сутностей.
2. **Через `db.Entry(entity).Collection(e => e.NavList).Load()`** — використовується для колекційних навігаційних властивостей (`List<T>`).
3. **Через `db.Entry(entity).Reference(e => e.NavProp).Load()`** — використовується для одиночних посилальних навігаційних властивостей.

### Додаткові можливості `Query()`

Методи `Collection()` і `Reference()` надають метод `.Query()`, який повертає `IQueryable<T>`. Це дозволяє:
- порахувати кількість дочірніх елементів на рівні SQL (`Count()`) без їхнього завантаження в пам'ять;
- відфільтрувати або відсортувати дочірні елементи перед завантаженням (`.Where(...).Load()`).

## Ключові фрагменти коду

```csharp
// 1. Завантаження через Where(...).Load()
var company = db.Companies.FirstOrDefault(c => c.Name == "Microsoft");
if (company != null)
{
    db.Users.Where(u => u.CompanyId == company.Id).Load();
    // Тепер company.Users автоматично заповнена!
}
```

```csharp
// 2. Використання Entry().Collection() та Query()
var count = db.Entry(company).Collection(c => c.Users).Query().Count();
db.Entry(company).Collection(c => c.Users).Load();

// 3. Використання Entry().Reference() для одиночної властивості
var user = db.Users.FirstOrDefault(u => u.Name == "Bob");
if (user != null)
{
    db.Entry(user).Reference(u => u.Company).Load();
}
```

## Особливості роботи в SQLite

- Кожен виклик `.Load()` або `.Query().Count()` виконує окремий параметризований SQL-запит до SQLite (наприклад, `SELECT ... FROM "Users" AS "u" WHERE "u"."CompanyId" = @__p_0`).
- У SQLite, на відміну від деяких конфігурацій SQL Server, не потрібно вмикати `MARS` (Multiple Active Result Sets), оскільки ми спершу матеріалізуємо головну сутність (`FirstOrDefault()`), а потім виконуємо наступний запит.

## Підсумок та підводні камені

- Для `Entry().Collection()` та `Entry().Reference()` сутність **повинна відстежуватися (tracked)** контекстом `DbContext`. Якщо вона була завантажена з `AsNoTracking()`, виклик `Load()` викине виняток.
- Explicit Loading ідеально підходить, коли пов'язані дані потрібні лише за певної умови (наприклад, залежно від ролі користувача або бізнес-логіки), щоб не перевантажувати початковий `SELECT` зайвими `JOIN`-ами.
- Не викликайте `Load()` у циклі `foreach` для великої кількості записів — це створить проблему **N+1 запитів**. У таких випадках краще використати `Include()`.
