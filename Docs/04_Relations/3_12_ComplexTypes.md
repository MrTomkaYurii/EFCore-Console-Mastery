# 3.12. Комплексні типи (Complex Types)

- **Глава:** 04. Відносини між моделями
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/3.12.php](https://metanit.com/sharp/efcore/3.12.php)
- **Код прикладу:** [Lesson_3_12_ComplexTypes.cs](../../src/04_Relations/Lesson_3_12_ComplexTypes.cs)

## Мета уроку

Ознайомитися з **комплексними типами (Complex Types)**, які з'явилися в EF Core 8+, та зрозуміти їхню ключову відмінність від Owned Types.

## Теоретичний матеріал

**Комплексні типи (`[ComplexType]` / `ComplexProperty()`)** — це сучасний спосіб моделювання *Value Objects* у EF Core (починаючи з версії 8.0).

### Основні ознаки Complex Types

- Не мають первинного ключа та ідентичності.
- Існують лише як частина іншої сутності (без власного `DbSet<T>`).
- Можуть бути як посилальними (`class`), так і значимими (`struct` / `record struct`) типами.
- **Спільне використання (Sharing):** на один і той самий екземпляр комплексного типу в C# можуть одночасно посилатися властивості різних сутностей (наприклад, `tom.Language = csharp`, `bob.Language = csharp`, `microsoft.Language = csharp`).

### Порівняння: `Owned Types` vs `Complex Types`

| Критерій | Owned Types (`[Owned]` / `OwnsOne`) | Complex Types (`[ComplexType]` / `ComplexProperty`) |
|---|---|---|
| Версія EF Core | З EF Core 2.0 | З EF Core 8.0+ |
| Підтримка `struct` | Ні (лише `class`) | Так (`class` та `struct`) |
| Спільний екземпляр для кількох сутностей | Ні (кожна сутність має свій екземпляр) | Так (можна передавати той самий об'єкт) |
| Колекції (`List<T>`) | Підтримуються (`OwnsMany`) | У EF Core 8–9 не підтримувались (з'являється підтримка в новіших версіях / JSON) |
| Зберігання в таблиці | Стовпці з префіксом у таблиці власника | Стовпці з префіксом у таблиці сутності |

## Ключові фрагменти коду

```csharp
[ComplexType]
public class Language
{
    public required string Name { get; set; }
}

public class User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required Language Language { get; set; }
}
```

```csharp
var csharp = new Language { Name = "C#" };
var tom = new User { Name = "Tom", Language = csharp };
var bob = new User { Name = "Bob", Language = tom.Language };
var microsoft = new Company { Name = "Microsoft", Language = csharp };
```

## Особливості роботи в SQLite

- У SQLite комплексні типи відображаються як звичайні стовпці в таблицях відповідних сутностей: `"Language_Name" TEXT NOT NULL` у таблиці `Users` та `"Language_Name" TEXT NOT NULL` у таблиці `Companies`.
- Жодних зовнішніх ключів чи проміжних таблиць для `ComplexType` у SQLite не створюється.

## Підсумок та підводні камені

- Якщо ви змінюєте властивість спільного об'єкта `csharp.Name = "C# (.NET)"` у пам'яті до `SaveChanges()`, зміняться всі сутності, які посилаються на цей об'єкт у поточному контексті. Але **після завантаження з БД у новому контексті** кожна сутність (`tom`, `bob`, `microsoft`) отримає власну незалежну копію об'єкта `Language` зі своїх стовпців у БД!
- Комплексні типи в EF Core 8/9 вимагають, щоб властивість була не `null` (у новіших версіях EF Core підтримку nullable complex properties розширено, але для надійності краще використовувати `required` або ініціалізувати за замовчуванням).
