using System;
using Microsoft.Extensions.DependencyInjection;
using StarWars.Characters;
using StarWars.Repositories;
using StarWars.Reviews;
using HotChocolate.Execution.Configuration;

namespace StarWars.Common
{
    public static class StarWarsGraphQLConfigure
    {
        public static IServiceCollection AddStarWarsServices(this IServiceCollection services)
        {
            // Add the custom services like repositories etc ...
            services.AddSingleton<ICharacterRepository, CharacterRepository>();
            services.AddSingleton<IReviewRepository, ReviewRepository>();
            return services;
        }

        public static IRequestExecutorBuilder ConfigureStarWarsGraphQLServer(this IRequestExecutorBuilder builder)
        {
            // Add GraphQL Services
            //Updated to Initialize StarWars with new v11+ configuration...
            builder
                .AddQueryType(d => d.Name("Query"))
                .AddMutationType(d => d.Name("Mutation"))
                //Disabled Subscriptions for v11+ and Azure Functions Example due to 
                //  supportability in Serverless architecture...
                //.AddSubscriptionType(d => d.Name("Subscription"))
                .AddType<CharacterQueries>()
                .AddType<ReviewQueries>()
                .AddType<ReviewMutations>()
                //Disabled Subscriptions for v11+ and Azure Functions Example due to 
                //  supportability in Serverless architecture...
                //.AddType<ReviewSubscriptions>()
                .AddType<Human>()
                .AddType<Droid>()
                .AddType<Starship>()
                //Now Required in v11+ to support the Attribute Usage (e.g. you may see the
                //  error: No filter convention found for scope `none`
                .AddFiltering()
                .AddSorting();

            return builder;
        }
    }
}
