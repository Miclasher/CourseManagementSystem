using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Assignments;
using Domain.Courses;
using Domain.Curriculum;
using SharedKernel;

namespace Application.Assignments.Grade;

internal sealed class GradeSubmissionCommandHandler(
    IAssignmentRepository assignments,
    ICurriculumRepository curriculum,
    ICourseRepository courses,
    IUserContext userContext)
    : ICommandHandler<GradeSubmissionCommand>
{
    public async Task<Result> Handle(GradeSubmissionCommand command, CancellationToken cancellationToken)
    {
        Submission? submission = await assignments.GetSubmissionByIdAsync(command.SubmissionId, cancellationToken);

        if (submission is null)
        {
            return Result.Failure(AssignmentErrors.SubmissionNotFound(command.SubmissionId));
        }

        Assignment? assignment = await assignments.GetByIdAsync(submission.AssignmentId, cancellationToken);
        Lesson? lesson = assignment is null
            ? null
            : await curriculum.GetLessonByIdAsync(assignment.LessonId, cancellationToken);

        if (assignment is null || lesson is null)
        {
            return Result.Failure(AssignmentErrors.NotFound(submission.AssignmentId));
        }

        if (!await courses.IsInstructorAsync(lesson.CourseId, userContext.UserId, cancellationToken))
        {
            return Result.Failure(CourseErrors.NotInstructor(lesson.CourseId));
        }

        if (command.Score > assignment.MaxScore)
        {
            return Result.Failure(AssignmentErrors.ScoreOutOfRange(assignment.MaxScore));
        }

        submission.Grade(command.Score, command.Feedback, userContext.UserId);
        assignments.Grade(submission);

        return Result.Success();
    }
}
