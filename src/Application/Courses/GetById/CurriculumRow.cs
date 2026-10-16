namespace Application.Courses.GetById;

internal sealed class CurriculumRow
{
    public Guid ModuleId { get; set; }
    public string ModuleTitle { get; set; } = string.Empty;
    public int ModulePosition { get; set; }
    public Guid? LessonId { get; set; }
    public string? LessonTitle { get; set; }
    public int? LessonPosition { get; set; }
    public int? DurationMinutes { get; set; }
}
