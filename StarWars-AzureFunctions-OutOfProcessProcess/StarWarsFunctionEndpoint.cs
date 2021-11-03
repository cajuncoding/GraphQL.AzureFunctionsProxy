using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HotChocolate.AzureFunctionsProxy;
using System.Threading;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

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
        private readonly IHttpContextAccessor _httpContextAccessor;

        public StarWarsFunctionEndpoint(IGraphQLAzureFunctionsExecutorProxy graphQLExecutorProxy, IHttpContextAccessor httpContextAccessor)
        {
            _graphQLExecutorProxy = graphQLExecutorProxy;
            _httpContextAccessor = httpContextAccessor;
        }

        [Function(nameof(StarWarsFunctionEndpoint))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "graphql")] HttpRequestData req,
            ILogger logger,
            CancellationToken cancellationToken
        )
        {
            logger.LogInformation("C# GraphQL Request processing via Serverless AzureFunctions...");

            return await _graphQLExecutorProxy.ExecuteFunctionsQueryAsync(
                _httpContextAccessor.HttpContext,
                logger,
                cancellationToken
            ).ConfigureAwait(false);
        }
    }
}
