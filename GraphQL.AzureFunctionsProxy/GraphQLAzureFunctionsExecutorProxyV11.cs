using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Serialization;

namespace HotChocolate.AzureFunctionsProxy
{
    /// <summary>
    ///BBernard
    ///This class that provides a proxy between Azure Functions and the existing HotChocolate Http middleware
    ///  for both GET & POST request processing.
    /// </summary>
    public class GraphQLAzureFunctionsExecutorProxyV11 : IGraphQLAzureFunctionsExecutorProxy
    {
        protected GraphQLAzureFunctionsMiddlewareProxy AzureFunctionsMiddlewareProxy { get; }

        public GraphQLAzureFunctionsExecutorProxyV11(
            IRequestExecutorResolver graphqlExecutorResolver,
            IHttpResultSerializer graphqlResultSerializer,
            IHttpRequestParser graphqlRequestParser,
            NameString schemaName = default
        )
        {
            this.AzureFunctionsMiddlewareProxy = new GraphQLAzureFunctionsMiddlewareProxy(
                graphqlExecutorResolver,
                graphqlResultSerializer,
                graphqlRequestParser,
                schemaName
            );
        }

        public async Task<IActionResult> ExecuteFunctionsQueryAsync(HttpContext httpContext, ILogger logger, CancellationToken cancellationToken)
        {
            try
            {

                //Use the Middleware Proxy to Invoke the pre-configured pipeline for Http POST & GET processing...
                var httpMiddlewareProxy = this.AzureFunctionsMiddlewareProxy;
                await httpMiddlewareProxy.InvokeAsync(httpContext);

            }
            //NOTE: We Implement error handling that matches the Existing HttpPostMiddleware, to ensure that all code
            //  has top level error handling for Graph processing.
            catch (GraphQLRequestException ex)
            {
                //If Debugging is enabled then Log the Errors to AzureFunctions framework (e.g. Application Insights)
                logger.LogDebug(ex, $"{nameof(GraphQLRequestException)} occurred while processing the GraphQL request; {ex.Message}.");

                // A GraphQL request exception is thrown if the HTTP request body couldn't be parsed.
                // In this case we will return HTTP status code 400 and return a GraphQL error result.
                IErrorHandler errorHandler = await this.AzureFunctionsMiddlewareProxy.GetErrorHandlerAsync(cancellationToken);
                IQueryResult errorResult = QueryResultBuilder.CreateError(errorHandler.Handle(ex.Errors));
               
                await HandleGraphQLErrorResponseAsync(httpContext, HttpStatusCode.BadRequest, errorResult);
            }
            catch (Exception exc)
            {
                //Log all Unknown Exceptions as GraphQLExceptions to Azure Framework (e.g. Application Insights).
                logger.LogError(exc, "An unhandled exception occurred while processing the GraphQL request.");

                // An unknown and unexpected GraphQL request exception was encountered.
                // In this case we will return HTTP status code 500 and return a GraphQL error result.
                IErrorHandler errorHandler = await this.AzureFunctionsMiddlewareProxy.GetErrorHandlerAsync(cancellationToken);
                IError error = errorHandler.CreateUnexpectedError(exc).Build();
                IQueryResult errorResult = QueryResultBuilder.CreateError(error);
                
                HttpStatusCode statusCode = exc is HttpRequestException 
                    ? HttpStatusCode.BadRequest 
                    : HttpStatusCode.InternalServerError;

                await HandleGraphQLErrorResponseAsync(httpContext, statusCode, errorResult);
            }

            //Safely resolve the .Net Core request with Empty Result because the Response has already been handled!
            return new EmptyResult();
        }

        /// <summary>
        /// Internal Error handler for the top level Execution process.
        /// </summary>
        /// <returns></returns>
        protected virtual async Task HandleGraphQLErrorResponseAsync(HttpContext httpContext, HttpStatusCode statusCode, IQueryResult errorResult)
        {
            // in any case we will have a valid GraphQL result at this point that can be written
            // to the HTTP response stream.
            Debug.Assert(errorResult != null, "No GraphQL result was created.");

            await this.AzureFunctionsMiddlewareProxy.WriteResultAsync(
                httpContext.Response, 
                errorResult, 
                statusCode, 
                httpContext.RequestAborted
            );
        }


    }
}
