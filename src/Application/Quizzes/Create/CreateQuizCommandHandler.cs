using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Courses;
using Domain.Curriculum;
using Domain.Quizzes;
using SharedKernel;

namespace Application.Quizzes.Create;

internal sealed class CreateQuizCommandHandler(
    ICurriculumRepository curriculum,
    ICourseRepository courses,
    IQuizRepository quizzes,
    IUserContext userContext)
    : ICommandHandler<CreateQuizCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateQuizCommand command, CancellationToken cancellationToken)
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

        var questions = command.Questions
            .Select(q => new QuizQuestion(q.Text, q.Options, q.CorrectOption, q.Points))
            .ToList();

        var quiz = Quiz.Create(lesson.Id, command.Title, command.PassPercent, command.MaxAttempts, questions);

        quizzes.Add(quiz);

        return quiz.Id;
    }
}
