namespace EFCoreConsoleMastery.Core;

/// <summary>
/// Базовий інтерфейс для кожного навчального прикладу (параграфа) курсу EF Core.
/// </summary>
public interface ILesson
{
    /// <summary>Послідовний номер параграфа у форматі "&lt;Глава&gt;.&lt;Урок&gt;" (наприклад, "1.1", "3.12").</summary>
    string Id { get; }

    /// <summary>Назва розділу (глави), до якого належить урок.</summary>
    string Chapter { get; }

    /// <summary>Назва параграфа українською мовою.</summary>
    string Title { get; }

    /// <summary>Посилання на оригінальну статтю на Metanit.</summary>
    string MetanitUrl { get; }

    /// <summary>Порядковий номер глави (1..8) для строгого сортування в меню.</summary>
    int ChapterOrder { get; }

    /// <summary>Порядковий номер уроку всередині глави для строгого сортування.</summary>
    int LessonOrder { get; }

    /// <summary>Запускає демонстраційний приклад у консолі.</summary>
    void Run();
}
