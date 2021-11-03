using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.AzureFunctionsProxy;
using HotChocolate.AzureFunctionsProxy;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace StarWars_AzureFunctions_OutOfProcessProcess2
{
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
            var graphQLFuncContext = new GraphQLAzFuncContext(executionContext);

            var logger = graphQLFuncContext.Logger;
            logger.LogInformation("C# GraphQL Request processing via Serverless AzureFunctions...");

            //TODO: Provide Proxy routing for BCP...
            var response = req.CreateResponse();
            await response.WriteStringAsync("NOT IMPLEMENTED...");

            return response;
        }
    }
}
