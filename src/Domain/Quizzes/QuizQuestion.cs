namespace Domain.Quizzes;

public sealed record QuizQuestion(string Text, List<string> Options, int CorrectOption, int Points);
