# GraphQL.AzureFunctionsProxy
## An (Unofficial) Extension pack for using HotChocolate GraphQL framework within Azure Functions for v11 & v12.

## Update Announcement -- Deprecated as of v13:
As of the new release of Hot Chocolate GraphQL Server v13, the Azure Functions Proxy and it's feature set are now an OOTB feature of Hot Chococlate -- along with some optimizations since it's part of the core code base! This was announced and shared in the [v13 introduction blog post here!](https://chillicream.com/blog/2023/02/08/new-in-hot-chocolate-13#azure-functions)

I collaborated closely with the core HC team to merge the valuable elements of the `GraphQL.AzureFunctionsProxy` library into HC and enable support for both In-Process and Isolated Process Azure Functions as an OOTB feature of HC now! Therefore as of v13, this project will no longer be maintained as a separate library -- my efforts will be focused on maintaining the core functionality going forward with the HC team!

So if you're upgrading to v13 you’ll need to migrate (it’s very easy) to use the OOTB version [as outlined here in the quick migration guide...](migrate_to_hc_v13.md)


**Update Notes for v12.5.0.0**
- Added Support for new HC v12.5 which changed middleware signatures; enabling support for OpenTelemetry, etc.

_Prior Release Notes:_
- Bumped to HC v12.4.1
- Added Build Target for .Net 6.0 now that it's fully supported for In-Process Azure Functions!
- Updated Star Wars InProcess Demo app to .Net 6 with Azuzr Functions v4.0.
- Added marshalling of RequestData.ClaimsIdentities into HttpContext ClaimsPrincipal for IsolatedProcess support in GraphQL.AzureFunctionsProxy.IsolatedProcess; now accessible from HttpContext.User.
- Added Support for Azure Functions using .Net 5 in the [Isolated Process Model (Out-of-process)](https://docs.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide).
  - This allows the Isolated Process to have the same support for features/functionality of HC as the in-process model including:
    - GraphQL execution with maximum out-of-the-box support (e.g.BatchRequests, Extension values, etc...)
    - Schema Download via ?SDL
    - Banana Cake Pop Web IDE
  - **Now available via Nuget Package:** [GraphQL.AzureFunctionsProxy.IsolatedProcess](https://www.nuget.org/packages/GraphQL.AzureFunctionsProxy.IsolatedProcess/)
    - This package provides the necessary adaptation for the Function Request/Response model for HotChocolate to Consume, but is as easy as calling the overload (custom extension) that takes in the `HttpRequestData` provided by the isolated process.
    - [Example below here...](#b-isolated-process-model-example)
- Added fully functioning [demonstration Project using the .Net 5 in the isolated process model here](https://github.com/cajuncoding/GraphQL.AzureFunctionsProxy/tree/main/StarWars-AzureFunctions-IsolatedProcess).
- Updated for HotChocolate v12 support!
- Enhanced StarWars Demo for testing/validating with v12 for additional manual tests.
- With this major update, I'm now correctly calling the GraphQL IDE "Banana Cake Pop" instead of "Playground" (which is the Old IDE).
- GraphQL IDE naming is now updated consistently to be called BananaCakePop (new IDE from v11) in code, comments, and Readme; updated Readme sample code also.
- Deprecated as obsolete the old EnablePlaygroundWebApp option which will be removed in future release; but remains in place and supported at this time for easier upgrade/transition of projects using the AzureFunctionsProxy.
- Added support for ?SDL download of the Schema (?SDL)
- Added support for Functioning GraphQL IDE *(Banana Cake Pop)* (when configured correctly iin the AzureFunction HttpTrigger route binding & new path option).
- Reduced the number of awaits used in the Middleware proxy for performance.
- Maintained compatibility with v11.0.4.
- Added ConfigureAwait(false) to all awaits for performance.
- Bumped to HC v11.0.4
- Updated to HC v11.0.1.1 due to critical fixes in HC v11.0.1 that resolve an issue in HC core that had broken Interfaces (which impacted the accompanying Star Wars Demo)
- Updated and released Nuget update to support namespace changes in v11 rc3!
- Updated Repo & Package names to eliminate conflicts with the core HotChocolate packages.

_Note: The versioning of this package is in sync with HC v11 releases as neeed, but with a fourth incrmenting revision for this package._

## Overview
This is a extension package for HotChocolate GraphQL framework to enable execution
within AzureFunctions using a simple Proxy so that all original functionality of functions
endpoints are unaffected.

This is **Unofficial** but working for most common use cases.

This also includes a working example of the StarWars Project running as an Azure Function
and modified only as needed to run as expected (with v11 API)!

### [Buy me a Coffee ☕](https://www.buymeacoffee.com/cajuncoding)
*I'm happy to share with the community, but if you find this useful (e.g for professional use), and are so inclinded,
then I do love-me-some-coffee!*

<a href="https://www.buymeacoffee.com/cajuncoding" target="_blank">
<img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174">
</a> 

### Nuget Package (>=.netcoreapp3.1, Azure Functions v3)
To use this as-is in your project, add the [GraphQL.AzureFunctionsProxy](https://www.nuget.org/packages/GraphQL.AzureFunctionsProxy) NuGet package to your project.
 and wire up your Starup and AzureFunction endpoint as outlined below...


## Demo Site (Star Wars)
This project contains a clone of the HotChocolate GraphQL *Star Wars* example project (Annotation based version; Pure Code First)
running as an AzureFunctions app and mildly updated to use the new v11 API. 

HotChocolate has changed the Execution pipeline for v11 API in many ways, and existing AzureFunctions
implementation samples don't account for various common use cases like BatchRequests, etc.

### NOTES: 
1. **NOTE:** According to the HotChocolate team on Slack, they will provide an Official AzureFunctions 
middleware as part of v11 (eventually). However it will be based on the cutting edge version of 
Azure Functions that enable running/initializing a project exactly like a normal AspNerCore app. 
So this library may still help address gaps in existing Azure Function projects :-)
2. **NOTE: Moderate Testing has been done on this and we are actively using it on projects, 
and will update with any findings; we have not completed exhaustive testing of all HotChocolate functionality.**

## Goals

* To provide a working approach to using the new **v11 API** until an official Middleware
is provided. 
* Keep this code fully encapsulated so that switching to the official Middleware will be as
painless and simple as possible *(with a few design assumptions aside)*.
* Keep this adaptation layer as lean and DRY as possible while also supporting as much OOTB
functionality as possible.
* Ensures that the Azure Functions paradigm and flexibility are not lost, so that all OOTB 
C# bindings, DI, and current Function invocation are maintained.

## Implementation:
This approach uses a "Middleware Proxy" pattern whereby we provide the functionality of the 
existing HotChocolate HTTP middleware via a proxy class that can be injected into the Azure Function,
but otherwise do not change the existing AzureFunctions invocation pipeline or the HotChocolate pipeline
as it is configured in the application startup class.

This Proxy exposes an "executor" interface that can process the HttpContext in an AzureFunction.
However, any pre/post logic could be added before/after the invocation of the executor proxy 
*IGraphQLAzureFunctionsExecutorProxy*.

This proxy is setup by internally configuring a Middleware Proxy that is an encapsulation of the 
existing HotChocolate *HttpPostMiddleware* & *HttpGetMiddleware* configured as a simple pipeline for processing POST 
requests first and then defaulting back to GET requests, and erroring out if neither are able to 
handle the request.

## Key Elements:
### Startup Configuration
1. The following Middleware initializer must be added into a valid AzureFunctions Configuration 'Startup.cs'
   - All other elements of HotChocolate initialization are the same using the v11 API.
   - **In-Process Model**: this is done in the `Startup.cs` class `Configure()` method.
   - **Isolated Process Model**: this is done in the `Program.cs` class `Main()` method.
##### The namespace for AzureFunctionsProxy is:
```csharp
using HotChocolate.AzureFunctionsProxy
```

```csharp
        //Finally Initialize AzureFunctions Executor Proxy here...
        services.AddAzureFunctionsGraphQL();
```

##### Or to enable/disable new features for Schema Download (?SDL) or GraphQL IDE *(Banana Cake Pop)* (DEFAULT is enabled):
```csharp
        //Finally Initialize AzureFunctions Executor Proxy here...
        services.AddAzureFunctionsGraphQL((options) =>
        {
            options.AzureFunctionsRoutePath = "/api/graphql"; //Default value is already `/api/graphql`
            options.EnableSchemaDefinitionDownload = true; //Default is already Enabled (true)
            options.EnableBananaCakePop = true; //Default is already Enabled (true)
            options.EnableGETRequests = true; //Default is already Enabled (true)
        });
```

### A) In Process Model: Azure Function Setup (Proxy the Requests from the Function into HC Pipeline)

**2A.** Dependency Inject the new **IGraphQLAzureFunctionsExecutorProxy** into the Function Endpoint:
   - NOTE: This is the same for both Azure Functions **in-process** and **isolated process** models...
```csharp
using HotChocolate.AzureFunctionsProxy
using....

public class StarWarsFunctionEndpoint
{
    private readonly IGraphQLAzureFunctionsExecutorProxy _graphqlExecutorProxy;

    public StarWarsFunctionEndpoint(IGraphQLAzureFunctionsExecutorProxy graphqlExecutorProxy)
    {
        _graphqlExecutorProxy = graphqlExecutorProxy;
    }
```

**3A.** Finally, the **IGraphQLAzureFunctionsExecutorProxy** can be invoked in the AzureFunction invocation:
```csharp
        [FunctionName(nameof(StarWarsFunctionEndpoint))]
        public async Task<IActionResult> Run(
            //NOTE: The Route must be configured to match wildcard path for the GraphQL IDE *(Banana Cake Pop)* to function properly.
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "graphql/{*path}")] HttpRequest req,
            ILogger logger,
            CancellationToken cancellationToken
        )
        {
            logger.LogInformation("C# GraphQL Request processing via Serverless AzureFunctions...");

            return await _graphqlExecutorProxy.ExecuteFunctionsQueryAsync(
                req.HttpContext,
                logger,
                cancellationToken
            ).ConfigureAwait(false);
        }
```

### B) [Isolated Process Model (Out-of-Process)](https://docs.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide): Azure Function Setup (Proxy the Requests from the Function into HC Pipeline)

**2B.** Dependency Inject the new **IGraphQLAzureFunctionsExecutorProxy** into the Function Endpoint:
   - _NOTE: See the additional `using HotChocolate.AzureFunctionsProxy.IsolatedProcess` to pull in the extensions needed for the Isolated Process Model._
```csharp
using HotChocolate.AzureFunctionsProxy;
using HotChocolate.AzureFunctionsProxy.IsolatedProcess;
using....

public class StarWarsFunctionEndpoint
{
    private readonly IGraphQLAzureFunctionsExecutorProxy _graphqlExecutorProxy;

    public StarWarsFunctionEndpoint(IGraphQLAzureFunctionsExecutorProxy graphqlExecutorProxy)
    {
        _graphqlExecutorProxy = graphqlExecutorProxy;
    }
```

**3B.**Finally, the **IGraphQLAzureFunctionsExecutorProxy** can be invoked in the AzureFunction invocation:
   - Notice this now consumes the `HttpRequestData` from the isolated process model, and returns the compatible `HttpResponseData`!
```csharp
        [Function(nameof(StarWarsFunctionEndpoint))]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "graphql")] HttpRequestData req,
            FunctionContext executionContext
        )
        {
            var logger = executionContext.GetLogger<StarWarsFunctionEndpoint>();
            logger.LogInformation("C# GraphQL Request processing via Serverless AzureFunctions (Isolated Process)...");

            var response = await _graphqlExecutorProxy.ExecuteFunctionsQueryAsync(req, logger).ConfigureAwait(false);
            return response;
        }
```


### Enabling the GraphQL IDE *(Banana Cake Pop)* via AzureFunctions
1. To enable the GraphQL IDE *(Banana Cake Pop)* the Azure Function must be configured properly to serve all Web Assets dynamically
for various paths, and the Middleware must be told what the AzureFunction path is via the Options configuration.

   - To do this, the HttpTrigger must be configured with wildcard matching on the path so that the Function will be bound
to all paths for processing (e.g. CSS, JavaScript, Manifest.json asset requests):

   - Take note of the ***/{\*path}*** component of the Route binding!
```csharp
        //NOTE: The Route must be configured to match wildcard path for the GraphQL IDE *(Banana Cake Pop)* to function properly.
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "graphql/bcp/{*path}")] HttpRequest req
```

2. Now it's a good idea to secure this Anonymous GraphQL IDE *(Banana Cake Pop)* endpoint to ensure that no data can
be served from this endpoing, which helps ensure that all data requests must be sent to the 
actual data endpoint that can be kept secure (e.g. `[HttpTrigger(AuthorizationLevel.Function...)]`):
   - **Note:** An full example of this is configured in the `StarWars-AzureFunctions` project.

#### A) In-Process Model Example:
```csharp
        [FunctionName(nameof(GraphQLBananaCakePopEndpoint))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "graphql/bcp/{*path}")] HttpRequest req,
            ILogger logger,
            CancellationToken cancellationToken
        )
        {
            logger.LogInformation("C# GraphQL Request processing via Serverless AzureFunctions...");

            //SECURE this endpoint against actual Data Queries
            //  This is useful for exposing the GraphQL IDE *(Banana Cake Pop)* anonymously, but keeping the actual GraphQL data endpoint
            //  secured with AzureFunction token security and/or other authorization approach.
            if (HttpMethods.IsPost(req.Method) || (HttpMethods.IsGet(req.Method) && !string.IsNullOrWhiteSpace(req.Query["query"])))
            {
                return new BadRequestErrorMessageResult("POST or GET GraphQL queries are invalid for the GraphQL IDE *(Banana Cake Pop)* endpoint.");
            }

            return await _graphqlExecutorProxy.ExecuteFunctionsQueryAsync(
                req.HttpContext,
                logger,
                cancellationToken
            ).ConfigureAwait(false);
        }
```

#### B) Isolated-Process Model Example:
  - Notice the use of extension method `req.GetQueryStringParam("query")` as needed to get the querystring property from the HttpRequestData object model in the isolated process. 
  - Unfortunately the `HttpRequestData` is a bare-bones abstraction of requeset and doesn't offer direct acccess the Querystring params, so the `using HotChocolate.AzureFunctionsProxy.IsolatedProcess` provides extensions and helpers that help provide functionality parity.
```csharp
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
```



1. Now with a valid Azure Function endpoing for our GraphQL IDE *(Banana Cake Pop)*, that is secured so that data cannot be queried,
we need to explicitly tell the AzureFunctionsProxy what the expected base url path is so that the HC Middleware 
will successfully serve all necessary resources/assets.
   - This is done easily by setting the `AzureFunctionsRoutePath` option in the configuration as follows:
     - **In-Process Model**: this is done in the `Startup.cs` class `Configure()` method.
     - **Isolated Process Model**: this is done in the `Program.cs` class `Main()` method.
   - Assuming the following then the configuration would be as follows:
     - This example assumes that you use a function `HttpTrigger` as defined above which allows running the GraphQL IDE *(Banana Cake Pop)* on it's own endpoint that is Anonymous;
       - This allows you keep the actual `/graphql` data endpoint secured with Azure  Functions Token security and/or other authorization approach.
     - *NOTE: The `HttpTrigger` Route binding for GraphQL IDE *(Banana Cake Pop)* MUST still use the wildcard path matching for it to function properly.*
```csharp
        // In HC startup configuration . . . 

        //Finally Initialize AzureFunctions Executor Proxy here...
        services.AddAzureFunctionsGraphQL((options) =>
        {
            //When accessing the GraphQL via AzureFunctions this is the path that all Urls will be prefixed with
            //  as configured in the AzureFunction host.json combined with the HttpTrigger Route binding.
            options.AzureFunctionsRoutePath = "/api/graphql/bcp";
        });
```

#### Additional GraphQL IDE *(Banana Cake Pop)* Usage Notes:
For GraphQL IDE *(Banana Cake Pop)* to function properly, with Azure Functions V2 using the proxy library, you 
will have to use Anonymous function security when deployed (for now?).  This is becuase the HC 
web app does not include the original querystring values for Function token ?code=123 when it 
makes requests for assets so they will fail with 401 Unauthorized.  Alternatively, a 
Chrome Plugin can be used to set the Token as a header value `x-functions-key`.  

*It works well with the ModHeader Chrome Extension.  However, to eliminate that dependency, I may 
look into making it easier to serve the GraphQL IDE *(Banana Cake Pop)* from an Anonymous Function (without 
exposing the actual data endpoitn) and/or creating  Bookmarklet/Favlet that does 
this without an extension in Chrome as time permits...*


## Disclaimers:
* GraphQL IDE *(Banana Cake Pop)* & Schema Download Functionality:
  - There is one key reason that GraphQL IDE *(Banana Cake Pop)* and Schema Download works -- because the DEFAULT values of HotChocolate's GraphQL Options are to Enable them!
    - *NOTE: In the Isolated Process Model there may be more control over the route details, but it has not be fully tested/validated.*
  - At this time the AzureFunctionsProxy can enable/disable the middleware by either wiring up the Middleware or not.
  - But, the HC middleware checks for GraphQL configured options that are set at Configuration build time to see if these are enable/disabled, 
and these options are stored on *Endpoint Metadata* that is **not accessible** (to my knowledge so far) in the Azure Function
  - This is because in Azure Functions (V2) we do not control the routing configuration at the same level of control that 
an Asp.Net Core applications do.
  - However, since the HC defaults are to be `Enabled = true` then it's a non-issue and everything works wonderfully!
* Subscriptsion were disabled in the example project due to unknown supportability in a 
serverless environment. 
  * The StarWars example uses in-memory subscriptions which are incongruent with the serverless
paradigm of AzureFunctions.

## Credits:
* Initial thoughts around design were adapted from [*OneCyrus'* Repo located here](https://github.com/OneCyrus/GraphQL-AzureFunctions-HotChocolate).
  * *OneCyrus'* example is designed around HotChocolate **v10 API** and the execution & configuration pipelines
    have changed significantly in the new **v11 API**.
  * This example also did not support BatchRequests, Extension values, etc... 
  * Therefore, rather than manually processing request as this prior example did, this approach 
    is different and leverages
alot more OOTB code from **HotChocolate.AspNetCore**
* The [HotChocolate Slack channel](https://join.slack.com/t/hotchocolategraphql/shared_invite/enQtNTA4NjA0ODYwOTQ0LTViMzA2MTM4OWYwYjIxYzViYmM0YmZhYjdiNzBjOTg2ZmU1YmMwNDZiYjUyZWZlMzNiMTk1OWUxNWZhMzQwY2Q)
was helpful for searching and getting some feedback to iron this out quickly.
