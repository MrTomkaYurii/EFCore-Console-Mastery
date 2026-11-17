# 2.5. Використання полів сутності

- **Глава:** 03. Створення моделей в EF Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/2.5.php](https://metanit.com/sharp/efcore/2.5.php)
- **Код прикладу:** [`Lesson_2_5_EntityFields.cs`](../../src/03_ModelsAndMapping/Lesson_2_5_EntityFields.cs)

## Мета уроку

Навчитися налаштовувати в EF Core зіставлення стовпців бази даних із приватними полями (backing fields) класу сутності — як за наявності властивості лише для читання, так і взагалі без публічної властивості.

## Теоретичний матеріал

У підходах Domain-Driven Design (DDD) часто потрібно інкапсулювати стан сутності, приховавши сеттери (`set`) або навіть самі властивості від зовнішнього коду. EF Core дозволяє працювати напряму з приватними полями класу через **Fluent API**.

Є два основні сценарії:
1. **Властивість лише для читання + приватне поле (`HasField`):**
   ```csharp
   modelBuilder.Entity<User>().Property("Id").HasField("id");
   ```
   EF Core читає і записує значення безпосередньо в приватне поле `id`, а зовнішній код бачить лише `public int Id => id;`.
2. **Тільки приватне поле без публічної властивості (`Property("fieldName")`):**
   ```csharp
   modelBuilder.Entity<User>().Property("name");
   ```
   Стовпець `name` створюється в таблиці, заповнюється під час матеріалізації (або через конструктор), а в LINQ-запитах до нього можна звертатися через `EF.Property<string>(u, "name")`.

## Ключові фрагменти коду

```csharp
public class User
{
    private int id = 0;
    private string name;
    private int age;

    public int Id => id;
    public int Age => age;

    public User(string name, int age)
    {
        this.name = name;
        this.age = age;
    }
}
```

## Особливості роботи в SQLite

- У таблиці `Users` у SQLite створюються стовпці `"Id"`, `"Age"` та `"name"` (останній — з назвою приватного поля).
- За потреби ім'я стовпця для поля `"name"` можна уточнити через `.HasColumnName("Name")`.

## Підсумок та підводні камені

- Імена властивостей і полів у `Property("...")` та `HasField("...")` передаються як рядки — помилка в імені призведе до винятку під час побудови моделі (`InvalidOperationException`).
- Якщо приватне поле заповнюється лише через EF Core (наприклад, `private int id;`), компілятор C# може видавати попередження `CS0649`; достатньо ініціалізувати його значенням за замовчуванням (`= 0;`).
