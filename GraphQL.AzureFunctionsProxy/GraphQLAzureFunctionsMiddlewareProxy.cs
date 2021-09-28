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
using Microsoft.Extensions.FileProviders;

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

        protected GraphQLAzureFunctionsConfigOptions Options { get; }
        protected IRequestExecutorResolver ExecutorResolver { get; }
        protected IHttpResultSerializer ResultSerializer { get; }
        protected IHttpRequestParser RequestParser { get; }
        protected IFileProvider FileProvider { get; }
        protected PathString RoutePath { get; }
        protected NameString SchemaName { get; }

        protected MiddlewareBase PrimaryMiddleware { get; }
        protected RequestDelegate MiddlewareProxyDelegate { get; set; }

        public GraphQLAzureFunctionsMiddlewareProxy(
            IRequestExecutorResolver graphQLExecutorResolver,
            IHttpResultSerializer graphQLResultSerializer,
            IHttpRequestParser graphQLRequestParser,
            NameString schemaName = default,
            GraphQLAzureFunctionsConfigOptions options = null
        )
        {
            //We support multiple schemas by allowing a name to be specified, but default to DefaultName if not.
            this.SchemaName = schemaName.HasValue ? schemaName : Schema.DefaultName;

            //Initialize the Server Options with defaults!
            this.Options = options ?? new GraphQLAzureFunctionsConfigOptions();

            //Validate Dependencies...
            this.ExecutorResolver = graphQLExecutorResolver ??
                throw new ArgumentNullException(nameof(graphQLExecutorResolver), GRAPHQL_MIDDLEWARE_INIT_ERROR);

            this.ResultSerializer = graphQLResultSerializer ??
                throw new ArgumentNullException(nameof(graphQLResultSerializer), GRAPHQL_MIDDLEWARE_INIT_ERROR);

            this.RequestParser = graphQLRequestParser ??
                throw new ArgumentNullException(nameof(graphQLRequestParser), GRAPHQL_MIDDLEWARE_INIT_ERROR);

            //The File Provider is initialized internally as an EmbeddedFileProvider
            this.FileProvider = GraphQLInitHelpers.CreateEmbeddedFileProvider();

            //Set the RoutePath; a dependency of all dynamic file serving Middleware!
            this.RoutePath = new PathString(Options.AzureFunctionsRoutePath);
            
            //Initialize the Primary Middleware (POST) as needed for references to GetExecutorAsync() for Error Handling, etc....
            //NOTE: This will also return the Middleware to be used as the primary reference.
            this.PrimaryMiddleware = ConfigureMiddlewareChainOfResponsibility();
        }

        private MiddlewareBase ConfigureMiddlewareChainOfResponsibility()
        {
            //*********************************************
            //Manually Build the Proxy Pipeline...
            //*********************************************
            this.MiddlewareProxyDelegate = (httpContext) => throw new HttpRequestException(
                "GraphQL was unable to process the request, ensure that an Http POST or GET GraphQL request was sent as well-formed Json."
            );

            //BBernard - Initialize the middleware proxy and pipeline with support for both Http GET & POST processing...
            //NOTE: Middleware uses the Pipeline Pattern (e.g. Chain Of Responsibility), therefore
            //  we adapt that here to manually build up the key middleware handlers for Http Get & Http Post processing.
            //NOTE: Other key features such as Schema download and the GraphQL IDE (Banana Cake Pop Dynamic UI) are all
            //      delivered by other Middleware also.
            //NOTE: Middleware MUST be configured in the correct order of dependency to support the functionality and
            //          the chain of responsibility is executed inside-out; or last registered middleware will run first and
            //          the first one registered will run last (if not already previously handled).
            //      Therefore we MUST register the middleware in reverse order of how the HC Core code does in the
            //          `public static GraphQLEndpointConventionBuilder MapGraphQL()` builder logic.
            //      Proper Execution Order must be:
            //          - HttpPostMiddleware
            //          - HttpGetSchemaMiddleware
            //          - ToolDefaultFileMiddleware
            //          - ToolOptionsFileMiddleware
            //          - ToolStaticFileMiddleware
            //          - HttpGetMiddleware

            if (Options.EnableGETRequests)
            {
                var httpGetMiddlewareShim = new HttpGetMiddleware(
                    this.MiddlewareProxyDelegate,
                    this.ExecutorResolver,
                    this.ResultSerializer,
                    this.RequestParser,
                    this.SchemaName
                );
                this.MiddlewareProxyDelegate = (httpContext) => httpGetMiddlewareShim.InvokeAsync(httpContext);
            }

            if (Options.EnableBananaCakePop)
            {
                var toolStaticFileMiddlewareShim = new ToolStaticFileMiddleware(
                    this.MiddlewareProxyDelegate,
                    this.FileProvider,
                    this.RoutePath
                );
                this.MiddlewareProxyDelegate = (httpContext) => toolStaticFileMiddlewareShim.Invoke(httpContext);

                var toolOptionsFileMiddlewareShim = new ToolOptionsFileMiddleware(
                    //New for v12 The Constructor for this Middleware is simplified and no longer requires the injection of GraphQL dependencies because it's simply a File Handler for BCP
                    this.MiddlewareProxyDelegate,
                    this.RoutePath
                );
                this.MiddlewareProxyDelegate = (httpContext) => toolOptionsFileMiddlewareShim.Invoke(httpContext);

                var toolDefaultFileMiddlewareShim = new ToolDefaultFileMiddleware(
                    this.MiddlewareProxyDelegate,
                    this.FileProvider,
                    this.RoutePath
                );
                this.MiddlewareProxyDelegate = (httpContext) => toolDefaultFileMiddlewareShim.Invoke(httpContext);
            }

            if (Options.EnableSchemaDefinitionDownload)
            {
                var httpGetSchemaMiddlewareShim = new HttpGetSchemaMiddleware(
                    this.MiddlewareProxyDelegate,
                    this.ExecutorResolver,
                    this.ResultSerializer,
                    this.SchemaName,
                    //New v12 parameter set to Integrated to enable integrated/default functionality (compatible with v11 behavior).
                    MiddlewareRoutingType.Integrated
                );
                this.MiddlewareProxyDelegate = (httpContext) => httpGetSchemaMiddlewareShim.InvokeAsync(httpContext);
            }

            ////NOTE: The normal use case for GraphQL is POST'ing of the query so we initialize it last in the chain/pipeline
            ////  so that it is the first to execute, and then fallback to Http Get if appropriate, finally throw
            ////  an exception if neither are supported by the current request.
            var httpPostMiddlewareShim = new HttpPostMiddleware(
                this.MiddlewareProxyDelegate,
                this.ExecutorResolver,
                this.ResultSerializer,
                this.RequestParser,
                this.SchemaName
            );
            this.MiddlewareProxyDelegate = (httpContext) => httpPostMiddlewareShim.InvokeAsync(httpContext);

            //RETURN the Post Middleware as the Primary Middleware reference...
            return httpPostMiddlewareShim;
        }

        /// <summary>
        /// Invoke the Middleware pipeline - will pass the context into the Azure Functions pre-configured
        /// pipeline to the HttpPostMiddleware first, and then to the HttpGetMiddlware if necessary.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public virtual Task InvokeAsync(HttpContext httpContext)
        {
            return this.MiddlewareProxyDelegate.Invoke(httpContext);
        }

        /// <summary>
        /// Internal helper to retrieve the current Error handler needed for Error processing.
        /// Matches logic used by existing HttpPostMiddleware.
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IErrorHandler> GetErrorHandlerAsync(CancellationToken cancellationToken)
        {
            IRequestExecutor requestExecutor = await this.PrimaryMiddleware.GetExecutorAsync(cancellationToken).ConfigureAwait(false);

            //Unable to use the HotChocolate Helper method GetErrorHandler() from RequestExecutorExtensions
            //  because it is marked as internal only, however, we can directly use the DI Provider in the same way.
            IErrorHandler errorHandler = requestExecutor.Services.GetRequiredService<IErrorHandler>();
            return errorHandler;
        }

        /// <summary>
        /// Borrowed this code to make error handling easier for writing errors to the Response
        /// in the same way that the existing Http Get/Post Middleware does.
        /// 
        /// NOTE: Attempted to inherit from HttpPostMiddleware to expose existing method, but due to
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
