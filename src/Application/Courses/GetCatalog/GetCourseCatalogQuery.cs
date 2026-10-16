using Application.Abstractions.Messaging;

namespace Application.Courses.GetCatalog;

public sealed record GetCourseCatalogQuery : IQuery<List<CatalogItemResponse>>;
