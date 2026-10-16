using Application.Abstractions.Messaging;
using Domain.Courses;

namespace Application.Courses.Update;

public sealed class UpdateCourseCommand : ICommand
{
    public Guid CourseId { get; set; }
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CourseLevel Level { get; set; }
    public decimal Price { get; set; }
    public int? MaxStudents { get; set; }
}
