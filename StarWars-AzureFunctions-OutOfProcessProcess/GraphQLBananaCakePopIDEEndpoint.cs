using System;
using System.Threading.Tasks;
using HotChocolate.AzureFunctionsProxy;
using HotChocolate.AzureFunctionsProxy.IsolatedProcess;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace StarWars.AzureFunctionsIsolatedProcess
{
    /// <summary>
    /// AzureFunction Endpoint for the Star Wars GraphQL Schema queries and Banana Cake Pop binary/asset handling
    /// NOTE: This class is not marked as static so that .Net Core DI handles injecting the Executor Proxy for us.
    /// </summary>
    public class GraphQLBananaCakePopEndpoint
    {
        private readonly IGraphQLAzureFunctionsExecutorProxy _graphqlExecutorProxy;

        public GraphQLBananaCakePopEndpoint(IGraphQLAzureFunctionsExecutorProxy graphqlExecutorProxy)
        {
            _graphqlExecutorProxy = graphqlExecutorProxy;
        }

        [Function(nameof(GraphQLBananaCakePopEndpoint))]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "graphql/bcp/{*path}")] HttpRequestData req,
            FunctionContext executionContext
        )
        {
            var logger = executionContext.GetLogger<GraphQLBananaCakePopEndpoint>();
            logger.LogInformation("C# GraphQL Request processing via Serverless AzureFunctions (Isolated Process)...");

            //SECURE this endpoint against actual Data Queries
            //  This is useful for exposing the GraphQL IDE (Banana Cake Pop) anonymously, but keeping the actual GraphQL data endpoint
            //  secured with AzureFunction token security and/or other authorization approach.

            if (HttpMethods.IsPost(req.Method) || (HttpMethods.IsGet(req.Method) && !string.IsNullOrWhiteSpace(req.GetQueryStringParam("query"))))
            {
                return req.CreateBadRequestErrorMessageResponse("POST or GET GraphQL queries are invalid for the GraphQL IDE endpoint.");
            }

            var response = await _graphqlExecutorProxy.ExecuteFunctionsQueryAsync(req, logger).ConfigureAwait(false);
            return response;
        }
    }
}
