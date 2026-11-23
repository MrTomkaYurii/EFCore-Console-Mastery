namespace EFCoreConsoleMastery.Core.Domain;

/// <summary>Навчальний курс для зв'язку M:N (таблиця dbo.Courses).</summary>
public class Course
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Student> Students { get; set; } = new();
}
