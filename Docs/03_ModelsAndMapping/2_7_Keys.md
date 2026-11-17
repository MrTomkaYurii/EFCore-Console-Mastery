# 2.7. Налаштування ключів

- **Глава:** 03. Створення моделей в EF Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/2.7.php](https://metanit.com/sharp/efcore/2.7.php)
- **Код прикладу:** [`Lesson_2_7_Keys.cs`](../../src/03_ModelsAndMapping/Lesson_2_7_Keys.cs)

## Мета уроку

Навчитися налаштовувати первинні ключі (Primary Keys) за угодами, через атрибут `[Key]` і Fluent API `HasKey()`, задавати складені ключі та альтернативні унікальні ключі (`HasAlternateKey`).

## Теоретичний матеріал

### Первинний ключ за угодою

За замовчуванням EF Core шукає властивість із назвою `Id` або `[Ім'яКласу]Id` (наприклад, `UserId`).

### Перевизначення первинного ключа

- **Анотація даних:** атрибут `[Key]` над потрібною властивістю.
- **Fluent API:**
  ```csharp
  modelBuilder.Entity<User>()
      .HasKey(u => u.Ident)
      .HasName("UsersPrimaryKey");
  ```
  Метод `HasName()` задає ім'я обмеження `PRIMARY KEY` у базі даних.

### Складені ключі (Composite Keys)

Складений ключ формується з кількох властивостей. **Його можна задати лише через Fluent API** (атрибут `[Key]` для кількох властивостей у такому вигляді не працює; у новіших версіях є `[PrimaryKey]`, але Fluent API є стандартом):

```csharp
modelBuilder.Entity<Citizen>()
    .HasKey(c => new { c.PassportSeria, c.PassportNumber });
```

### Альтернативні ключі (Alternate Keys)

Альтернативний ключ — це властивість (або набір властивостей), яка не є первинним ключем, але також повинна мати унікальне значення і може слугувати ціллю для зовнішнього ключа:

```csharp
modelBuilder.Entity<User>()
    .HasAlternateKey(u => u.Passport);
```

На рівні БД це створює обмеження `UNIQUE` (та `NOT NULL` для властивості).

## Ключові фрагменти коду

```csharp
modelBuilder.Entity<User>()
    .HasKey(u => u.Ident)
    .HasName("UsersPrimaryKey");

modelBuilder.Entity<User>()
    .HasAlternateKey(u => u.Passport);
```

## Особливості роботи в SQLite

- У SQLite складений первинний ключ генерується як `CONSTRAINT "PK_Citizens" PRIMARY KEY("PassportSeria", "PassportNumber")`.
- Альтернативний ключ генерує `CONSTRAINT "AK_Users_Passport" UNIQUE("Passport")`, а стовпець `Passport` автоматично стає `NOT NULL`, навіть якщо в C# він був оголошений як `string?`.

## Підсумок та підводні камені

- Властивості, що входять до альтернативного ключа (`HasAlternateKey`), автоматично стають **обов'язковими (`NOT NULL`)** і **незмінними (read-only після вставки)** у EF Core. Якщо вам потрібна просто унікальність із можливістю зміни значення чи `NULL`, краще використовувати унікальний індекс (`HasIndex(...).IsUnique()`).
