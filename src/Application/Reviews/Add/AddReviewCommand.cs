using Application.Abstractions.Messaging;

namespace Application.Reviews.Add;

public sealed record AddReviewCommand(Guid CourseId, short Rating, string? Comment) : ICommand<Guid>;
