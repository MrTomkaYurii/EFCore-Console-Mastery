namespace EFCoreConsoleMastery.Core.Domain;

/// <summary>Студент для зв'язку M:N (таблиця dbo.Students).</summary>
public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Course> Courses { get; set; } = new();
}
