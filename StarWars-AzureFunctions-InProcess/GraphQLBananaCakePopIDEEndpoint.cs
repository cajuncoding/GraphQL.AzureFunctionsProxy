using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HotChocolate.AzureFunctionsProxy;
using System.Threading;
using System.Web.Http;

namespace StarWars.AzureFunctions
{
    /// <summary>
    /// AzureFunction Endpoint for the Star Wars GraphQL Schema queries and Banana Cake Pop binary/asset handling
    /// NOTE: This class is not marked as static so that .Net Core DI handles injecting the Executor Proxy for us.
    /// </summary>
    public class GraphQLBananaCakePopEndpoint
    {
        private readonly IGraphQLAzureFunctionsExecutorProxy _graphQLExecutorProxy;

        public GraphQLBananaCakePopEndpoint(IGraphQLAzureFunctionsExecutorProxy graphQLExecutorProxy)
        {
            _graphQLExecutorProxy = graphQLExecutorProxy;
        }

        [FunctionName(nameof(GraphQLBananaCakePopEndpoint))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "graphql/bcp/{*path}")] HttpRequest req,
            ILogger logger,
            CancellationToken cancellationToken
        )
        {
            logger.LogInformation("C# GraphQL Request processing via Serverless AzureFunctions...");

            //SECURE this endpoint against actual Data Queries
            //  This is useful for exposing the GraphQL IDE (Banana Cake Pop) anonymously, but keeping the actual GraphQL data endpoint
            //  secured with AzureFunction token security and/or other authorization approach.
            if (HttpMethods.IsPost(req.Method) || (HttpMethods.IsGet(req.Method) && !string.IsNullOrWhiteSpace(req.Query["query"])))
            {
                return new BadRequestErrorMessageResult("POST or GET GraphQL queries are invalid for the GraphQL IDE endpoint.");
            }

            var actionResult = await _graphQLExecutorProxy.ExecuteFunctionsQueryAsync(
                req.HttpContext, 
                logger, 
                cancellationToken
            ).ConfigureAwait(false);

            return actionResult;
        }
    }
}
