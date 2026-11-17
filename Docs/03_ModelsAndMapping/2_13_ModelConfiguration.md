# 2.13. Конфігурація моделей (`IEntityTypeConfiguration`)

- **Глава:** 03. Створення моделей в EF Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/2.13.php](https://metanit.com/sharp/efcore/2.13.php)
- **Код прикладу:** [`Lesson_2_13_ModelConfiguration.cs`](../../src/03_ModelsAndMapping/Lesson_2_13_ModelConfiguration.cs)

## Мета уроку

Навчитися структурувати великі конфігурації Fluent API, виносячи налаштування кожної сутності в окремий клас через інтерфейс `IEntityTypeConfiguration<T>`, окремий метод-делегат або атрибут `[EntityTypeConfiguration]`.

## Теоретичний матеріал

Коли кількість сутностей у проєкті зростає, метод `OnModelCreating` у `DbContext` стає надто громіздким. EF Core пропонує три способи декомпозиції конфігурації:

1. **Окремі класи з `IEntityTypeConfiguration<T>` + `ApplyConfiguration()`:**
   ```csharp
   modelBuilder.ApplyConfiguration(new UserConfiguration());
   ```
   (У великих проєктах також зручно використовувати `modelBuilder.ApplyConfigurationsFromAssembly(...)`, який автоматично знаходить усі реалізації `IEntityTypeConfiguration<T>` у збірці).
2. **Окремі методи в `DbContext`:**
   ```csharp
   modelBuilder.Entity<User>(UserConfigure);
   ```
3. **Атрибут `[EntityTypeConfiguration(typeof(OrderConfiguration))]`:**
   Ставиться безпосередньо над класом сутності — тоді `OnModelCreating` взагалі не потрібно перевизначати.

## Ключові фрагменти коду

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("People");
        builder.Property(p => p.Id).HasColumnName("user_id");
        builder.Property(p => p.Name).IsRequired();
    }
}
```

## Особливості роботи в SQLite

- Організація конфігурацій за допомогою `IEntityTypeConfiguration<T>` є суто архітектурним механізмом EF Core і працює абсолютно однаково для SQLite та будь-якої іншої СУБД.

## Підсумок та підводні камені

- **Увага до `ApplyConfigurationsFromAssembly`:** У навчальних проєктах, де в одній збірці міститься багато різних `DbContext` для різних уроків, викликати `ApplyConfigurationsFromAssembly` не можна, бо кожен контекст підтягне конфігурації сутностей з інших уроків. Тому в цьому проєкті ми використовуємо точковий `ApplyConfiguration(new ...())` або `[EntityTypeConfiguration]`.
