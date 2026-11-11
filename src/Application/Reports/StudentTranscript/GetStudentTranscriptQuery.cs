using Application.Abstractions.Messaging;

namespace Application.Reports.StudentTranscript;

public sealed record GetStudentTranscriptQuery(ReportSource Source, Guid StudentId) : IQuery<StudentTranscriptResponse>;
