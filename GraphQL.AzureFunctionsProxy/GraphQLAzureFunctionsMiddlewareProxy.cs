using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using HotChocolate.Language;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using HotChocolate.AspNetCore.Serialization;

namespace HotChocolate.AzureFunctionsProxy
{
    /// <summary>
    ///BBernard
    ///This class that provides a proxy between Azure Functions and the existing HotChocolate Http middleware
    ///  for both GET & POST request processing.  It initializes the Http POST & GET middleware and exposes
    ///  useful helper methods to keep this code DRY.
    /// </summary>
    public class GraphQLAzureFunctionsMiddlewareProxy
    {
        public const string GRAPHQL_MIDDLEWARE_INIT_ERROR = "Ensure that the services.AddGraphQLServer() has been initialized first.";

        protected IRequestExecutorResolver ExecutorResolver { get; }
        protected IHttpResultSerializer ResultSerializer { get; }
        protected IHttpRequestParser RequestParser { get; }
        protected NameString SchemaName { get; }
        protected HttpPostMiddleware MiddlewareProxy { get; }

        public GraphQLAzureFunctionsMiddlewareProxy(
            IRequestExecutorResolver graphqlExecutorResolver,
            IHttpResultSerializer graphqlResultSerializer,
            IHttpRequestParser graphqlRequestParser,
            NameString schemaName = default
        )
        {
            //We support multiple schemas by allowing a name to be specified, but default to DefaultName if not.
            this.SchemaName = schemaName.HasValue ? schemaName : Schema.DefaultName;

            //Validate Dependencies...
            this.ExecutorResolver = graphqlExecutorResolver ??
                throw new ArgumentNullException(nameof(graphqlExecutorResolver), GRAPHQL_MIDDLEWARE_INIT_ERROR);

            this.ResultSerializer = graphqlResultSerializer ??
                throw new ArgumentNullException(nameof(graphqlResultSerializer), GRAPHQL_MIDDLEWARE_INIT_ERROR);

            this.RequestParser = graphqlRequestParser ??
                throw new ArgumentNullException(nameof(graphqlRequestParser), GRAPHQL_MIDDLEWARE_INIT_ERROR);


            //BBernard - Initialize the middleware proxy and pipeline with support for both Http GET & POST processing...
            //NOTE: Middleware uses the Pipeline Pattern (similar to Chain Of Responsibility), therefore
            //  we adapt that here to manually build up the two key middleware handlers for Http Get & Http Post processing.
            var httpGetMiddlewareShim = new HttpGetMiddleware(
                (httpContext) => throw new HttpRequestException(
                    "GraphQL was unable to process the request, ensure that an Http POST or GET GraphQL request was sent as well-formed Json."
                ),
                this.ExecutorResolver,
                this.ResultSerializer,
                this.RequestParser,
                this.SchemaName
            );

            ////NOTE: The normal use case for GraphQL is POST'ing of the query so we initialize it last in the chain/pipeline
            ////  so that it is the first to execute, and then fallback to Http Get if appropriate, finally throw
            ////  an exception if neither are supported by the current request.
            this.MiddlewareProxy = new HttpPostMiddleware(
                async (httpContext) => await httpGetMiddlewareShim.InvokeAsync(httpContext).ConfigureAwait(false),
                this.ExecutorResolver,
                this.ResultSerializer,
                this.RequestParser,
                this.SchemaName
            );
        }

        /// <summary>
        /// Invoke the Middleware pipeline - will pass the context into the Azure Functions pre-configured
        /// pipeline to the HttpPostMiddleware first, and then to the HttpGetMiddlware if necessary.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public virtual async Task InvokeAsync(HttpContext httpContext)
        {
            await this.MiddlewareProxy.InvokeAsync(httpContext).ConfigureAwait(false);
        }

        /// <summary>
        /// Internal helper to retrieve the current Error handler needed for Error processing.
        /// Matches logic used by existing HttpPostMiddlware.
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IErrorHandler> GetErrorHandlerAsync(CancellationToken cancellationToken)
        {
            IRequestExecutor requestExecutor = await this.MiddlewareProxy.GetExecutorAsync(cancellationToken).ConfigureAwait(false);

            //Unable to use the HotChocolate Helper method GetErrorHandler() from RequestExecutorExtensions
            //  because it is marked as internal only, however, we can directly use the DI Provider in the same way.
            IErrorHandler errorHandler = requestExecutor.Services.GetRequiredService<IErrorHandler>();
            return errorHandler;
        }

        /// <summary>
        /// Borrowed this code to make error handling easier for writing errors to the Response
        /// in the same way that the existing Http Get/Post Middleware does.
        /// 
        /// NOTE: Attempted to inherit from HttpPostMiddlware to expose existing method, but due to
        ///   constructor chaining order of processing and next() pipeline references being private in
        ///   the base class, it wasn't effectively possible without resorting to brute-force reflection,
        ///   so we duplicate some small amount of code here to provide the same Write logic for writing
        ///   responses -- just like other Middleware currently have.
        /// 
        /// </summary>
        /// <param name="response"></param>
        /// <param name="result"></param>
        /// <param name="statusCode"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async ValueTask WriteResultAsync(
            HttpResponse response,
            IExecutionResult result,
            HttpStatusCode? statusCode,
            CancellationToken cancellationToken
        )
        {
            var resultSerializer = this.ResultSerializer;
            response.ContentType = resultSerializer.GetContentType(result);
            response.StatusCode = (int)(statusCode ?? resultSerializer.GetStatusCode(result));

            await resultSerializer.SerializeAsync(result, response.Body, cancellationToken).ConfigureAwait(false);
        }

    }
}
