# 2.1. Моделі, Fluent API та анотації даних

- **Глава:** 03. Створення моделей в EF Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/2.1.php](https://metanit.com/sharp/efcore/2.1.php)
- **Код прикладу:** [`Lesson_2_1_ModelsAndFluentApi.cs`](../../src/03_ModelsAndMapping/Lesson_2_1_ModelsAndFluentApi.cs)

## Мета уроку

Зрозуміти, що таке модель в Entity Framework Core та які три механізми використовуються для її налаштування і зіставлення C#-класів із таблицями бази даних: умовні угоди (Conventions), анотації даних (Data Annotations) та Fluent API.

## Теоретичний матеріал

**Модель** в Entity Framework Core — це набір усіх сутностей (класів C#) і зв'язків між ними, якими керує контекст даних `DbContext`.

Для побудови моделі EF Core надає три підходи:

| Підхід | Де описується | Переваги | Недоліки / обмеження |
| :--- | :--- | :--- | :--- |
| **Умовні угоди (Conventions)** | Автоматичні правила EF Core | Мінімум коду, працює «з коробки» | Не покриває нестандартні імена чи складні обмеження |
| **Анотації даних (Data Annotations)** | Атрибути у класах моделей (`System.ComponentModel.DataAnnotations`) | Наочність прямо біля властивостей, зручно для валідації в UI/API | Обмежені можливості (наприклад, немає складених ключів); змішує доменну модель із деталями БД |
| **Fluent API** | Метод `OnModelCreating(ModelBuilder)` у `DbContext` | Повна потужність конфігурації, чистота доменних класів, найвищий пріоритет | Потребує більше коду в `DbContext` або окремих класів конфігурації |

### Пріоритет конфігурації

Якщо одні й ті самі параметри задані різними способами, діє правило пріоритету (від найнижчого до найвищого):
1. **Умовні угоди (Conventions)** — застосовуються базово.
2. **Анотації даних (Data Annotations)** — перевизначають угоди.
3. **Fluent API** — перевизначає і угоди, і анотації даних.

### Fluent API

Для використання Fluent API у класі контексту даних перевизначається метод `OnModelCreating(ModelBuilder modelBuilder)`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>()
        .Property(u => u.Name)
        .HasDefaultValue("Anonymous");

    base.OnModelCreating(modelBuilder);
}
```

### Анотації даних

Анотації даних — це атрибути з просторів імен `System.ComponentModel.DataAnnotations` та `System.ComponentModel.DataAnnotations.Schema`. Наприклад, атрибут `[Column("user_id")]` змінює ім'я стовпця в таблиці.

## Ключові фрагменти коду

```csharp
public class User
{
    [Column("user_id")]
    public int Id { get; set; }

    public string? Name { get; set; }
    public int Age { get; set; }
}
```

Після виклику `EnsureCreated()` у SQLite створюється таблиця `Users` зі стовпцем `"user_id"` замість `"Id"`.

## Особливості роботи в SQLite

- У прикладі на Metanit використовується SQLite (`UseSqlite`). У цьому проєкті рядок підключення формується через `SqliteDbHelper.GetConnectionString("ch03_l2_1")`, а для ізольованості уроків база перестворюється перед запуском.
- Анотація `[Column("user_id")]` та Fluent API працюють у SQLite так само, як і в SQL Server.

## Підсумок та підводні камені

- Для невеликих проєктів зручно поєднувати **Conventions** та **Data Annotations**, але для складної бізнес-логіки та чистої архітектури краще використовувати **Fluent API**.
- Пам'ятайте, що **Fluent API завжди перемагає анотації**, якщо вони суперечать одне одному.
