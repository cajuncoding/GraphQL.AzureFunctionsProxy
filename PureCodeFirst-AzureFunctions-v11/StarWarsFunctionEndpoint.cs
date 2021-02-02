using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using HotChocolate.AzureFunctionsProxy;
using System.Threading;

namespace StarWars.AzureFunctions
{
    /// <summary>
    /// AzureFunction Endpoint for the Star Wars GraphQL Schema queries
    /// NOTE: This class is not marked as static so that .Net Core DI handles injecting
    ///         the Executor Proxy for us.
    /// </summary>
    public class StarWarsFunctionEndpoint
    {
        private readonly IGraphQLAzureFunctionsExecutorProxy _graphQLExecutorProxy;

        public StarWarsFunctionEndpoint(IGraphQLAzureFunctionsExecutorProxy graphQLExecutorProxy)
        {
            _graphQLExecutorProxy = graphQLExecutorProxy;
        }

        [FunctionName(nameof(StarWarsFunctionEndpoint))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "graphql/{*path}")] HttpRequest req,
            ILogger logger,
            CancellationToken cancellationToken
        )
        {
            logger.LogInformation("C# GraphQL Request processing via Serverless AzureFunctions...");

            return await _graphQLExecutorProxy.ExecuteFunctionsQueryAsync(
                req.HttpContext,
                logger,
                cancellationToken
            ).ConfigureAwait(false);
        }
    }
}
