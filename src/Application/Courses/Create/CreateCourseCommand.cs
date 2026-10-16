using Application.Abstractions.Messaging;
using Domain.Courses;

namespace Application.Courses.Create;

public sealed class CreateCourseCommand : ICommand<Guid>
{
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CourseLevel Level { get; set; }
    public decimal Price { get; set; }
    public int? MaxStudents { get; set; }
}
