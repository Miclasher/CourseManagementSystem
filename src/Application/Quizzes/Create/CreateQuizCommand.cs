using Application.Abstractions.Messaging;

namespace Application.Quizzes.Create;

public sealed record CreateQuizCommand(
    Guid LessonId,
    string Title,
    int PassPercent,
    int MaxAttempts,
    List<QuestionDto> Questions) : ICommand<Guid>;

public sealed record QuestionDto(string Text, List<string> Options, int CorrectOption, int Points);
