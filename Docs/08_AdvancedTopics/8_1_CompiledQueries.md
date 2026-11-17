# 8.1. Скомпільовані запити (EF.CompileQuery / CompileAsyncQuery)

- **Глава:** 08. Додаткові теми
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/8.1.php](https://metanit.com/sharp/efcore/8.1.php)
- **Код прикладу:** [Lesson_8_1_CompiledQueries.cs](../../src/08_AdvancedTopics/Lesson_8_1_CompiledQueries.cs)

---

## Мета уроку

Зрозуміти, як працює кешування LINQ-запитів у Entity Framework Core, та навчитися підвищувати продуктивність високочастотних запитів за допомогою явної компіляції через `EF.CompileQuery` і `EF.CompileAsyncQuery`.

---

## Теоретичний матеріал

За замовчуванням EF Core автоматично компілює та кешує кожен LINQ-запит:
1. При кожному виклику запиту EF Core будує дерево виразів (`Expression Tree`).
2. Обчислює хеш цього дерева виразів.
3. Шукає готовий план трансляції в кеші.
4. Якщо не знайдено — транслює LINQ у SQL і зберігає в кеш.

Для більшості сценаріїв цього достатньо. Проте на "гарячих" ділянках коду (тисячі однакових запитів за секунду) етапи побудови дерева виразів, обчислення хешу та пошуку в кеші створюють зайві накладні витрати.

**Скомпільовані запити (`EF.CompileQuery` / `EF.CompileAsyncQuery`)** дозволяють виконати компіляцію **один раз** (зазвичай у `static readonly` полі) і отримати готовий делегат `Func<TContext, ..., TResult>`. При подальших викликах цього делегата EF Core миттєво підставляє параметри та виконує вже скомпільований запит.

### Порівняння методів компіляції

| Метод | Повертаємий тип делегата | Використання |
| :--- | :--- | :--- |
| `EF.CompileQuery` (один об'єкт) | `Func<TContext, ..., TEntity?>` | `FirstOrDefault`, `SingleOrDefault`, `Count`, `Any` тощо. |
| `EF.CompileQuery` (колекція) | `Func<TContext, ..., IEnumerable<TEntity>>` | Запити з `Where`, `OrderBy`, `Select` без виклику `ToList()` всередині. |
| `EF.CompileAsyncQuery` (один об'єкт) | `Func<TContext, ..., Task<TEntity?>>` | Асинхронне отримання одного значення (`FirstOrDefault(...)` у лямбді стає `Task<T>`). |
| `EF.CompileAsyncQuery` (колекція) | `Func<TContext, ..., IAsyncEnumerable<TEntity>>` | Потокове читання через `await foreach`. |

---

## Ключові фрагменти коду

Синхронний скомпільований запит із `Include` та `EF.Functions.Like`:

```csharp
private static readonly Func<CompiledQueriesContext, string, int, IEnumerable<User>> UsersByNameAndAgeQuery =
    EF.CompileQuery((CompiledQueriesContext db, string namePattern, int minAge) =>
        db.Users
            .Include(u => u.Company)
            .Where(u => EF.Functions.Like(u.Name!, namePattern) && u.Age > minAge));
```

Асинхронний скомпільований запит:

```csharp
private static readonly Func<CompiledQueriesContext, int, Task<User?>> UserByIdAsyncQuery =
    EF.CompileAsyncQuery((CompiledQueriesContext db, int id) =>
        db.Users
            .Include(u => u.Company)
            .FirstOrDefault(u => u.Id == id));
```

Зверніть увагу: всередині лямбди `EF.CompileAsyncQuery` використовується **синхронний** `FirstOrDefault(...)` (а не `FirstOrDefaultAsync`), а сам `CompileAsyncQuery` автоматично перетворює його на асинхронний делегат `Task<User?>`.

---

## Особливості роботи в SQLite

Скомпільовані запити працюють у SQLite **ідентично** до SQL Server та інших провайдерів.
- Функція `EF.Functions.Like(u.Name!, "%Tom%")` транслюється у стандартний SQLite-оператор `LIKE`.
- Якщо ви використовуєте локальний файл SQLite, основний час виконання все одно витрачається на файловий I/O (тому різниця між звичайним і скомпільованим запитом у невеликому консольному тесті може бути непомітною або коливатися в межах кількох мілісекунд — виграш відчутний передусім за CPU при високому навантаженні).

---

## Підсумок та підводні камені

- **Зберігайте делегати у `static readonly` полях:** якщо викликати `EF.CompileQuery(...)` щоразу всередині методу, запит буде компілюватися заново на кожен виклик, що зробить код повільнішим за звичайний LINQ.
- **Не викликайте `ToList()` або `FirstOrDefaultAsync()` всередині лямбди для колекцій:**
  - Для колекцій у `CompileQuery` повертайте `IQueryable<T>` (без `ToList()`), щоб делегат повертав `IEnumerable<T>` / `IAsyncEnumerable<T>`.
  - У `CompileAsyncQuery` для одиночних елементів використовуйте синхронний `FirstOrDefault(...)`, а не `FirstOrDefaultAsync(...)`.
- **Параметризація:** перший параметр лямбди — завжди ваш `DbContext`, а всі змінні фільтрації мають передаватися як параметри делегата (а не захоплюватися із зовнішніх локальних змінних).
