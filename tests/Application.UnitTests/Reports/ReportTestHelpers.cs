using System.Text.Json;
using Application.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace Application.UnitTests.Reports;

internal static class ReportTestHelpers
{
    public static IKeyedServiceProvider ProviderWith<TSource>(ReportSource key, TSource source)
        where TSource : class
    {
        IKeyedServiceProvider provider = Substitute.For<IKeyedServiceProvider>();
        provider.GetKeyedService(typeof(TSource), key).Returns(source);
        return provider;
    }

    public static IKeyedServiceProvider EmptyProvider() => Substitute.For<IKeyedServiceProvider>();

    public static string Json<T>(T value) => JsonSerializer.Serialize(value, JsonSerializerOptions.Web);
}
