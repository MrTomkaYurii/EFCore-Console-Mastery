# 3.6. Відношення багато до багатьох

- **Глава:** 04. Відносини між моделями
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/3.6.php](https://metanit.com/sharp/efcore/3.6.php)
- **Код прикладу:** [Lesson_3_6_ManyToMany.cs](../../src/04_Relations/Lesson_3_6_ManyToMany.cs)

## Мета уроку

Навчитися налаштовувати та використовувати зв'язок **багато до багатьох (Many-to-Many)** у EF Core без створення явного C#-класу для проміжної таблиці, а також перейменовувати проміжну таблицю через `UsingEntity`.

## Теоретичний матеріал

**Відношення багато до багатьох (M:N)** виникає, коли кожен об'єкт сутності A (`Student`) може бути пов'язаний із багатьма об'єктами сутності B (`Course`), і навпаки.

На рівні реляційної БД такий зв'язок завжди реалізується через **проміжну таблицю (junction / join table)**, яка містить складений первинний ключ із двох зовнішніх ключів. Починаючи з EF Core 5.0, у коді C# **не обов'язково** оголошувати проміжну сутність (якщо у зв'язку немає додаткових полів на кшталт `EnrollmentDate` чи `Grade`).

### Автоматична конфігурація vs `UsingEntity`

- **За конвенцією:** якщо `Course` має `List<Student> Students`, а `Student` — `List<Course> Courses`, EF Core автоматично створить проміжну таблицю `CourseStudent` зі стовпцями `CoursesId` і `StudentsId`.
- **Через Fluent API (`UsingEntity`):** дозволяє задати власну назву проміжної таблиці (наприклад, `Enrollments`), налаштувати імена стовпців або додати payload-властивості (додаткові колонки).

### Маніпуляція зв'язками

Щоб додати або видалити зв'язок, достатньо змінити колекцію з будь-якого боку (наприклад, `alice.Courses.Remove(algorithms); alice.Courses.Add(basics);`) і викликати `SaveChanges()`. EF Core сам згенерує `DELETE` та `INSERT` для проміжної таблиці.

## Ключові фрагменти коду

```csharp
public class Course
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public List<Student> Students { get; set; } = new();
}

public class Student
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public List<Course> Courses { get; set; } = new();
}
```

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Course>()
        .HasMany(c => c.Students)
        .WithMany(s => s.Courses)
        .UsingEntity(j => j.ToTable("Enrollments"));
}
```

## Особливості роботи в SQLite

- У SQLite проміжна таблиця `Enrollments` (або `CourseStudent`) створюється зі складеним первинним ключем `PRIMARY KEY("CoursesId", "StudentsId")` та двома `FOREIGN KEY` із `ON DELETE CASCADE`.
- При видаленні студента або курсу всі відповідні рядки з проміжної таблиці видаляються каскадно, тоді як самі курси чи студенти залишаються недоторканими.

## Підсумок та підводні камені

- Для оновлення колекції зв'язків (`alice.Courses.Remove(...)`) обов'язково попередньо завантажте її через `.Include(s => s.Courses)`, інакше EF Core не знатиме про поточні зв'язки в БД.
- Якщо зв'язок має власні атрибути (наприклад, дата запису, оцінка студента на курсі), краще створити явну сутність `Enrollment` і зв'язати її через два відношення `1:N` (або використати `UsingEntity<Enrollment>(...)`).
