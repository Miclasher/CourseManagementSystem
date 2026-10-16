using SharedKernel;

namespace Domain.Users;

public sealed record UserDeactivatedDomainEvent(Guid UserId) : IDomainEvent;
