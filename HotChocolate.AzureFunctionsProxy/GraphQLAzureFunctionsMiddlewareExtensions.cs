using HotChocolate.AspNetCore.Utilities;
using HotChocolate.AzureFunctionsProxy;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AzureFunctionsProxy
{
    public static class AzureFunctionsMiddlewareExtensions
    {
        /// <summary>
        /// Initialize an AzureFunctionsGraphQL Middleware for the optionally specified SchemaName.
        /// Default Schema name will be used if not specified.
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <param name="schemaName"></param>
        /// <returns></returns>
        public static IServiceCollection AddAzureFunctionsGraphQL(this IServiceCollection serviceCollection, NameString schemaName = default)
        {
            //serviceCollection.AddAzureFunctionsGraphQL(new AzureFunctionsMiddlewareOptions());
            serviceCollection.AddSingleton<IGraphQLAzureFunctionsExecutorProxy, GraphQLAzureFunctionsExecutorProxyV11>(
                provider => new GraphQLAzureFunctionsExecutorProxyV11(
                    provider.GetService<IRequestExecutorResolver>(),
                    provider.GetService<IHttpResultSerializer>(),
                    provider.GetService<IHttpRequestParser>(),
                    schemaName
                )
            );

            return serviceCollection;
        }
    }
}
