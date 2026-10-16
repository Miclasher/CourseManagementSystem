namespace Application.Reviews.GetByCourse;

public sealed class ReviewResponse
{
    public Guid Id { get; set; }
    public short Rating { get; set; }
    public string? Comment { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
