using Application.Abstractions.Messaging;
using Domain.Courses;
using SharedKernel;

namespace Application.Courses.Create;

internal sealed class CreateCourseCommandHandler(ICourseRepository courses)
    : ICommandHandler<CreateCourseCommand, Guid>
{
    public Task<Result<Guid>> Handle(CreateCourseCommand command, CancellationToken cancellationToken)
    {
        var course = Course.Create(
            command.CategoryId,
            command.Title,
            command.Slug,
            command.Description,
            command.Level,
            command.Price,
            command.MaxStudents);

        courses.Add(course);

        return Task.FromResult<Result<Guid>>(course.Id);
    }
}
