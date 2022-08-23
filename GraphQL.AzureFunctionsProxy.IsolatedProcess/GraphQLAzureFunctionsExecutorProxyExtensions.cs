using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HotChocolate.AzureFunctionsProxy.IsolatedProcess
{
    public static class GraphQLAzureFunctionsExecutorProxyExtensions
    {
        public static async Task<HttpResponseData> ExecuteFunctionsQueryAsync(
            this IGraphQLAzureFunctionsExecutorProxy graphqlExecutorProxy, 
            HttpRequestData httpRequestData, 
            ILogger log = null, 
            CancellationToken cancellationToken = default
        )
        {
            AssertParamIsNotNull(graphqlExecutorProxy, nameof(graphqlExecutorProxy));
            AssertParamIsNotNull(httpRequestData, nameof(httpRequestData));

            //Create the GraphQL HttpContext Shim to help marshall data from the Isolated Process HttpRequestData into a
            //  AspNetCore compatible HttpContext, and marshall results back into HttpResponseData from the HttpContext...
            await using var graphQLHttpContextShim = new GraphQLHttpContextShim(httpRequestData);

            //Build the Http Context Shim for HotChocolate to consume via the AzureFunctionsProxy...
            var graphqlHttpContextShim = await graphQLHttpContextShim.CreateGraphQLHttpContextAsync();

            var httpContextAccessor = httpRequestData.FunctionContext.InstanceServices.GetService<IHttpContextAccessor>();
            if (httpContextAccessor != null)
                httpContextAccessor.HttpContext = graphqlHttpContextShim;

            //Execute the full HotChocolate middleware pipeline via AzureFunctionsProxy...
            var logger = log ?? httpRequestData.FunctionContext.GetLogger<IGraphQLAzureFunctionsExecutorProxy>();

            await graphqlExecutorProxy.ExecuteFunctionsQueryAsync(graphqlHttpContextShim, logger, cancellationToken).ConfigureAwait(false);

            //Marshall the results back into the isolated process compatible HttpResponseData...
            var httpResponseData = await graphQLHttpContextShim.CreateHttpResponseDataAsync().ConfigureAwait(false);
            return httpResponseData;

        }

        private static void AssertParamIsNotNull(object value, string argName)
        {
            if (value == null)
                throw new ArgumentNullException(argName);
        }
    }
}
