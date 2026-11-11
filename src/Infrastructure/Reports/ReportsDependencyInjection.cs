using Application.Reports;
using Application.Reports.CourseDashboard;
using Application.Reports.MonthlyActivity;
using Application.Reports.StudentTranscript;
using Application.Reports.TopCourses;
using Infrastructure.Reports.ClickHouse;
using Infrastructure.Reports.Mongo;
using Infrastructure.Reports.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Infrastructure.Reports;

internal static class ReportsDependencyInjection
{
    public static IServiceCollection AddReports(this IServiceCollection services, IConfiguration configuration)
    {
        string mongoConnectionString = configuration.GetConnectionString("Mongo")!;
        string clickHouseConnectionString = configuration.GetConnectionString("ClickHouse")!;

        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
        services.AddSingleton(sp => new MongoReportsContext(
            sp.GetRequiredService<IMongoClient>(),
            MongoReportsContext.DatabaseNameOf(mongoConnectionString)));

        services.AddSingleton(_ => new ClickHouseConnectionFactory(clickHouseConnectionString));

        services.AddKeyedScoped<IMonthlyActivitySource, PostgresMonthlyActivitySource>(ReportSource.Postgres);
        services.AddKeyedScoped<ITopCoursesSource, PostgresTopCoursesSource>(ReportSource.Postgres);
        services.AddKeyedScoped<ICourseDashboardSource, PostgresCourseDashboardSource>(ReportSource.Postgres);
        services.AddKeyedScoped<IStudentTranscriptSource, PostgresStudentTranscriptSource>(ReportSource.Postgres);

        services.AddKeyedScoped<IMonthlyActivitySource, ClickHouseMonthlyActivitySource>(ReportSource.ClickHouse);
        services.AddKeyedScoped<ITopCoursesSource, ClickHouseTopCoursesSource>(ReportSource.ClickHouse);

        services.AddKeyedScoped<ICourseDashboardSource, MongoCourseDashboardSource>(ReportSource.Mongo);
        services.AddKeyedScoped<IStudentTranscriptSource, MongoStudentTranscriptSource>(ReportSource.Mongo);

        services
            .AddHealthChecks()
            .AddMongoDb(name: "mongodb")
            .AddCheck<ClickHouseHealthCheck>("clickhouse");

        return services;
    }
}
