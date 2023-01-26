using System;
using System.Threading.Tasks;
using HotChocolate.AzureFunctionsProxy;
using HotChocolate.AzureFunctionsProxy.IsolatedProcess;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace StarWars.AzureFunctionsIsolatedProcess
{
    public class StarWarsFunctionEndpoint
    {
        private readonly IGraphQLAzureFunctionsExecutorProxy _graphqlExecutorProxy;

        public StarWarsFunctionEndpoint(IGraphQLAzureFunctionsExecutorProxy graphqlExecutorProxy)
        {
            _graphqlExecutorProxy = graphqlExecutorProxy;
        }

        [Function(nameof(StarWarsFunctionEndpoint))]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "graphql")] HttpRequestData req,
            FunctionContext executionContext
        )
        {
            var logger = executionContext.GetLogger<StarWarsFunctionEndpoint>();
            logger.LogInformation("C# GraphQL Request processing via Serverless AzureFunctions (Isolated Process)...");

            req.Headers.Add("Wazzup", "YEAH the HttpContextAccessor works!");

            var response = await _graphqlExecutorProxy.ExecuteFunctionsQueryAsync(req, logger).ConfigureAwait(false);

            return response;
        }
    }
}
