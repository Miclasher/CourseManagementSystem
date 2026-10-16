namespace Application.Courses.GetCatalog;

public sealed class CatalogItemResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? LeadInstructorName { get; set; }
    public decimal Price { get; set; }
    public decimal RatingAvg { get; set; }
    public int EnrolledCount { get; set; }
    public int LessonCount { get; set; }
    public int TotalMinutes { get; set; }
}
