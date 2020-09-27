using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetApp.EntityFrameworkCore
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCSharpNotificationEntityImplementation(this IServiceCollection services)
        {
            return services
                .AddSingleton<ICSharpEntityTypeGenerator, CSharpNotificationEntityTypeGenerator>()
                .AddSingleton<ICSharpDbContextGenerator, CSharpNotificationStrategyDbContextGenerator>();
        }
    }
}
