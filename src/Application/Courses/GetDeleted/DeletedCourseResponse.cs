namespace Application.Courses.GetDeleted;

public sealed class DeletedCourseResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }
    public string? DeletedByName { get; set; }
}
