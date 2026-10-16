using Application.Abstractions.Messaging;

namespace Application.Curriculum.AddModule;

public sealed record AddModuleCommand(Guid CourseId, string Title) : ICommand<Guid>;
