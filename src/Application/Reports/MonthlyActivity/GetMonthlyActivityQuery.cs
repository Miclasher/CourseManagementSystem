using Application.Abstractions.Messaging;

namespace Application.Reports.MonthlyActivity;

public sealed record GetMonthlyActivityQuery(ReportSource Source, int Year) : IQuery<MonthlyActivityResponse>;
