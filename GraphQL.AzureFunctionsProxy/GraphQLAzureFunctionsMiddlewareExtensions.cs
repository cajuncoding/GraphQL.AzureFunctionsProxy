using System;
using HotChocolate.AspNetCore.Serialization;
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
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IServiceCollection AddAzureFunctionsGraphQL(this IServiceCollection serviceCollection,
            Action<GraphQLAzureFunctionsConfigOptions> configureOptions
        )
        {
            return serviceCollection.AddAzureFunctionsGraphQL(default, configureOptions: configureOptions);
        }

        /// <summary>
        /// Initialize an AzureFunctionsGraphQL Middleware for the optionally specified SchemaName.
        /// Default Schema name will be used if not specified.
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <param name="schemaName"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IServiceCollection AddAzureFunctionsGraphQL(this IServiceCollection serviceCollection, 
            NameString schemaName = default,
            Action<GraphQLAzureFunctionsConfigOptions> configureOptions = null
        )
        {
            //serviceCollection.AddAzureFunctionsGraphQL(new AzureFunctionsMiddlewareOptions());
            serviceCollection.AddSingleton<IGraphQLAzureFunctionsExecutorProxy, GraphQLAzureFunctionsExecutorProxyV11Plus>(
                provider => new GraphQLAzureFunctionsExecutorProxyV11Plus(
                    provider.GetService<IRequestExecutorResolver>(),
                    provider.GetService<IHttpResultSerializer>(),
                    provider.GetService<IHttpRequestParser>(),
                    schemaName,
                    GetConfiguredOptions(configureOptions)
                )
            );

            return serviceCollection;
        }

        private static GraphQLAzureFunctionsConfigOptions GetConfiguredOptions(Action<GraphQLAzureFunctionsConfigOptions> optionsAction)
        {
            if (optionsAction == null)
                return null;

            var options = new GraphQLAzureFunctionsConfigOptions();
            optionsAction?.Invoke(options);
            return options;
        }
    }
}
