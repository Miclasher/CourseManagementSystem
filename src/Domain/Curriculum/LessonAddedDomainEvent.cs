using SharedKernel;

namespace Domain.Curriculum;

public sealed record LessonAddedDomainEvent(Guid LessonId, Guid ModuleId) : IDomainEvent;
