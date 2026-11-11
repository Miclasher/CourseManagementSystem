namespace Application.Reports;

/// <summary>
/// Where a report is read from. Every report has a PostgreSQL implementation (computed live from the
/// normalized OLTP schema) and one NoSQL implementation (a denormalized read model projected from PostgreSQL).
/// </summary>
public enum ReportSource
{
    Postgres = 0,
    Mongo = 1,
    ClickHouse = 2
}
