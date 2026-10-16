namespace Application.Courses.Search;

public sealed class CourseSearchResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal RatingAvg { get; set; }
    public int EnrolledCount { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}
