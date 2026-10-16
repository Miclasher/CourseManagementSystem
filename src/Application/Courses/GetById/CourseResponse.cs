namespace Application.Courses.GetById;

public sealed class CourseResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal RatingAvg { get; set; }
    public int RatingCount { get; set; }
    public int EnrolledCount { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? LeadInstructorName { get; set; }
    public int LessonCount { get; set; }
    public int TotalMinutes { get; set; }
    public List<ModuleResponse> Modules { get; set; } = [];
}

public sealed class ModuleResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }
    public List<LessonResponse> Lessons { get; set; } = [];
}

public sealed class LessonResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }
    public int DurationMinutes { get; set; }
}
