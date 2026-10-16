using Application.Abstractions.Messaging;

namespace Application.Assignments.GetPending;

public sealed record GetPendingSubmissionsQuery : IQuery<List<PendingSubmissionResponse>>;
