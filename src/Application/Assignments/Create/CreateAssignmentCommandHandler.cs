using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Assignments;
using Domain.Courses;
using Domain.Curriculum;
using SharedKernel;

namespace Application.Assignments.Create;

internal sealed class CreateAssignmentCommandHandler(
    ICurriculumRepository curriculum,
    ICourseRepository courses,
    IAssignmentRepository assignments,
    IUserContext userContext)
    : ICommandHandler<CreateAssignmentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateAssignmentCommand command, CancellationToken cancellationToken)
    {
        Lesson? lesson = await curriculum.GetLessonByIdAsync(command.LessonId, cancellationToken);

        if (lesson is null)
        {
            return Result.Failure<Guid>(LessonErrors.NotFound(command.LessonId));
        }

        if (!await courses.IsInstructorAsync(lesson.CourseId, userContext.UserId, cancellationToken))
        {
            return Result.Failure<Guid>(CourseErrors.NotInstructor(lesson.CourseId));
        }

        var assignment = Assignment.Create(
            lesson.Id,
            command.Title,
            command.Description,
            command.MaxScore,
            command.DueAt?.ToUniversalTime());

        assignments.Add(assignment);

        return assignment.Id;
    }
}
