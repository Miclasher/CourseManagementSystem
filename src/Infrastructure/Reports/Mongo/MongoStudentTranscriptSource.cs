using Application.Reports.StudentTranscript;
using MongoDB.Driver;

namespace Infrastructure.Reports.Mongo;

internal sealed class MongoStudentTranscriptSource(MongoReportsContext context) : IStudentTranscriptSource
{
    public async Task<StudentTranscriptData?> GetAsync(Guid studentId, CancellationToken cancellationToken) =>
        await context.StudentTranscripts
            .Find(t => t.StudentId == studentId)
            .FirstOrDefaultAsync(cancellationToken);
}
