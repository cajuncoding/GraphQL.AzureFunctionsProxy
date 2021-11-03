using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using GraphQL.AzureFunctionsProxy;
using HotChocolate.AzureFunctionsProxy;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Enum = System.Enum;

namespace StarWars_AzureFunctions_OutOfProcessProcess2
{
    public class StarWarsFunctionEndpoint
    {
        private readonly IGraphQLAzureFunctionsExecutorProxy _graphqlExecutorProxy;

        public StarWarsFunctionEndpoint(IGraphQLAzureFunctionsExecutorProxy graphqlExecutorProxy)
        {
            _graphqlExecutorProxy = graphqlExecutorProxy;
        }

        [Function("StarWarsFunctionEndpoint")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "graphql")] HttpRequestData req,
            FunctionContext executionContext
        )
        {
            //TODO: Simplify into GraphQLHttpContext Shim taking in Request and Logger vs FunctionsContext...
            var graphQLFuncContext = new GraphQLAzFuncContext(executionContext);

            var logger = graphQLFuncContext.Logger;
            logger.LogInformation("C# GraphQL Request processing via Serverless AzureFunctions...");

            var contentType = req.Headers.TryGetValues(HeaderNames.ContentType, out var contentTypeHeaders)
                ? contentTypeHeaders.FirstOrDefault()
                : "application/json";

            var graphQLHttpRequest = graphQLFuncContext.CreateHttpRequest(
                HttpMethod.Post,
                requestBody: await req.ReadAsStringAsync(),
                requestContentType: contentType
            );

            var httpContext = graphQLFuncContext.HttpContext;

            await _graphqlExecutorProxy.ExecuteFunctionsQueryAsync(
                graphQLFuncContext.HttpContext,
                logger,
                CancellationToken.None
            ).ConfigureAwait(false);


            //TODO: PROXY Raw bytes to eliminate duplicate String processing...
            var graphqlResponseBytes = graphQLFuncContext.ReadResponseBytes();
            var httpStatusCode = (HttpStatusCode)httpContext.Response.StatusCode;

            var response = req.CreateResponse(httpStatusCode);
            var responseHeaders = httpContext.Response?.Headers;
            if (responseHeaders?.Any() == true)
                foreach (var header in responseHeaders)
                    response.Headers.Add(header.Key, header.Value.Select(sv => sv.ToString()));

            await response.WriteBytesAsync(graphqlResponseBytes);

            return response;

        }
    }
}
