using GraphQL.AzureFunctionsProxy.Tests.GraphQL;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using HotChocolate.AzureFunctionsProxy;

//CRITICAL: Here we self-wire up the Startup into the Azure Functions framework!
//[assembly: FunctionsStartup(typeof(GraphQL.AzureFunctionsProxy.Tests.HelloWorldFunctionsStartup))]

namespace GraphQL.AzureFunctionsProxy.Tests
{
    /// <summary>
    /// Startup middleware configurator specific for AzureFunctions
    /// </summary>
    public class HelloWorldFunctionsStartup : FunctionsStartup
    {
        // This method gets called by the AzureFunctions runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit:
        //  https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var services = builder.Services;

            // Add GraphQL Services
            //Updated to Initialize StarWars with v11+ configuration...
            services
                .AddGraphQLServer()
                .AddQueryType(d => d.Name("Query"))
                .AddType<HelloWorldResolver>();

            //Finally Initialize AzureFunctions Executor Proxy here...
            //You man Provide a specific SchemaName for multiple Functions (e.g. endpoints).
            //TODO: Test multiple SchemaNames...
            services.AddAzureFunctionsGraphQL((options) =>
            {
                options.AzureFunctionsRoutePath = "/api/graphql";
            });
        }
    }
}
