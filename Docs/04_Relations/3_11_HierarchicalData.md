# 3.11. Ієрархічні дані

- **Глава:** 04. Відносини між моделями
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/3.11.php](https://metanit.com/sharp/efcore/3.11.php)
- **Код прикладу:** [Lesson_3_11_HierarchicalData.cs](../../src/04_Relations/Lesson_3_11_HierarchicalData.cs)

## Мета уроку

Навчитися моделювати та завантажувати **ієрархічні (деревоподібні) структури даних** у EF Core за допомогою самопосилальних сутностей (Self-Referencing Entities).

## Теоретичний матеріал

Багато предметних областей мають деревоподібну структуру: пункти меню з підменю, категорії товарів, структура каталогів чи ієрархія співробітників (керівник → підлеглі).

У EF Core це моделюється через **самопосилальний зв'язок `1:N`**, де одна й та сама сутність виступає і як головна (батько), і як залежна (дитина):
- `int? ParentId` — **nullable** зовнішній ключ на батьківський елемент (для кореневих елементів дорівнює `null`).
- `MenuItem? Parent` — посилання на батьківський вузол.
- `List<MenuItem> Children` — колекція дочірніх вузлів.

### Як завантажувати дерево з БД

1. **Завантаження всієї таблиці в контекст (`db.MenuItems.ToList()`):** якщо дерево невелике, завантаження всіх рядків в один `DbContext` автоматично активує механізм **Relationship Fix-up** — EF Core сам заповнить усі `Parent` і `Children` у пам'яті без жодного `Include()`.
2. **Вибіркове завантаження через `Include()`:** `db.MenuItems.Include(m => m.Children)` завантажить лише один рівень вкладеності (або кілька рівнів через `ThenInclude`). Для довільної глибини вкладеності зазвичай або завантажують усе дерево в пам'ять, або використовують рекурсивні CTE (Common Table Expressions) через `FromSqlRaw`.

## Ключові фрагменти коду

```csharp
public class MenuItem
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public int? ParentId { get; set; }   // Nullable FK для кореневих вузлів
    public MenuItem? Parent { get; set; }
    public List<MenuItem> Children { get; set; } = new();
}
```

```csharp
// Завантаження всього дерева з автоматичним зв'язуванням у пам'яті
var allItems = db.MenuItems.ToList();
var roots = allItems.Where(m => m.ParentId == null).ToList();
```

## Особливості роботи в SQLite

- У SQLite генерується таблиця з самопосилальним ключем: `CONSTRAINT "FK_MenuItems_MenuItems_ParentId" FOREIGN KEY("ParentId") REFERENCES "MenuItems"("Id")` (без `ON DELETE CASCADE`, оскільки `ParentId` є nullable).
- Якщо ви захочете видалити батьківський вузол, який має дочірні елементи, у SQLite без явного видалення дітей або обнулення їхнього `ParentId` виникне помилка порушення обмеження `FOREIGN KEY`.

## Підсумок та підводні камені

- У самопосилальних зв'язках зовнішній ключ `ParentId` **обов'язково** має бути nullable (`int?`), інакше ви не зможете додати жодного кореневого елемента!
- `Include(m => m.Children)` завантажує лише **один** рівень дітей. Щоб побудувати глибоке дерево в пам'яті (як `File -> Save -> Save As...`), найпростіше завантажити всі вузли таблиці (або піддерево через CTE) в один відстежуваний `DbContext` і відфільтрувати корені `Where(m => m.ParentId == null)`.
