﻿using Microsoft.Extensions.DependencyInjection;
using HotChocolate;
using StarWars.Characters;
using StarWars.Repositories;
using StarWars.Reviews;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using HotChocolate.AzureFunctionsProxy;

//CRITICAL: Here we self-wire up the Startup into the Azure Functions framework!
[assembly: FunctionsStartup(typeof(StarWars.Startup))]

namespace StarWars
{
    /// <summary>
    /// Startup middleware configurator specific for AzureFunctions
    /// </summary>
    public class Startup : FunctionsStartup
    {
        // This method gets called by the AzureFunctions runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit:
        //  https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var services = builder.Services;

            // Add the custom services like repositories etc ...
            services.AddSingleton<ICharacterRepository, CharacterRepository>();
            services.AddSingleton<IReviewRepository, ReviewRepository>();

            // Add GraphQL Services
            //Updated to Initialize StarWars with new v11 configuration...
            services
                .AddGraphQLServer()
                .AddQueryType(d => d.Name("Query"))
                .AddMutationType(d => d.Name("Mutation"))
                //Disabled Subscriptions for v11 and Azure Functions Example due to 
                //  supportability in Serverless architecture...
                //.AddSubscriptionType(d => d.Name("Subscription"))
                .AddType<CharacterQueries>()
                .AddType<ReviewQueries>()
                .AddType<ReviewMutations>()
                //Disabled Subscriptions for v11 and Azure Functions Example due to 
                //  supportability in Serverless architecture...
                //.AddType<ReviewSubscriptions>()
                .AddType<Human>()
                .AddType<Droid>()
                .AddType<Starship>()
                //Now Required in v11 to support the Attribute Usage (e.g. you may see the
                //  error: No filter convention found for scope `none`
                .AddFiltering()
                .AddSorting();

            //Finally Initialize AzureFunctions Executor Proxy here...
            //You man Provide a specific SchemaName for multiple Functions (e.g. endpoints).
            //TODO: Test multiple SchemaNames...
            services.AddAzureFunctionsGraphQL((options) =>
            {
                //The Path must match the exact routing path that the Azure Function HttpTrigger is bound to.
                //NOTE: ThIs includes the /api/ prefix unless it was specifically removed or changed in the host.json file.
                //NOTE: THe default value is `/api/graphql`, but it's being done here to illustrate how to set the value.
                options.AzureFunctionsRoutePath = "/api/graphql/playground";
            });
        }
    }
}
