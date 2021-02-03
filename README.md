# GraphQL.AzureFunctionsProxy
## An (Unofficial) Extension pack for using HotChocolate GraphQL framework within Azure Functions for v11.

**Update Notes:**
- Added support for ?SDL download of the Schema (?SDL)
- Added support for Functioning Playground (when configured correctly iin the AzureFunction HttpTrigger route binding & new path option).
- Reduced the number of awaits used in the Middleware proxy for performance.
- Maintained compatibility with v11.0.4.

Prior Release Notes:
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
```csharp
        //Finally Initialize AzureFunctions Executor Proxy here...
        services.AddAzureFunctionsGraphQL();
```

  - Or to enable/disable new features for Schema Download (?SDL) or Playground (DEFAULT is enabled):
```csharp
        //Finally Initialize AzureFunctions Executor Proxy here...
        services.AddAzureFunctionsGraphQL((options) =>
        {
            options.AzureFunctionsRoutePath = "/api/graphql"; //Default value is already `/api/graphql`
            options.EnableSchemaDefinitionDownload = true; //Default is already Enabled (true)
            options.EnablePlaygroundWebApp = true; //Default is already Enabled (true)
            options.EnableGETRequests = true; //Default is already Enabled (true)
        });
```

  * Note: The namespace for this new middleware and proxy classes as needed is:
```csharp
using HotChocolate.AzureFunctionsProxy
```

2. Dependency Inject the new **IGraphQLAzureFunctionsExecutorProxy** into the Function Endpoint:
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

3. Finally, the **IGraphQLAzureFunctionsExecutorProxy** can be invoked in the AzureFunction invocation:
```csharp
        [FunctionName(nameof(StarWarsFunctionEndpoint))]
        public async Task<IActionResult> Run(
            //NOTE: The Route must be configured to match wildcard path for the Playground to function properly.
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

### Enabling the Playground from AzureFunctions
1. To enable the Playground the Azure Function must be configured properly to serve all Web Assets dynamically
for various paths, and the Middleware must be told what the AzureFunction path is via the Options configuration.

   - To do this, the HttpTrigger must be configured with wildcard matching on the path so that the Function will be bound
to all paths for processing (e.g. CSS, JavaScript, Manifest.json asset requests):

   - Take note of the ***/{\*path}*** component of the Route binding!
```csharp
        //NOTE: The Route must be configured to match wildcard path for the Playground to function properly.
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "graphql/{*path}")] HttpRequest req,
```

2. If you use the standard AzureFunctions configuration and map your function to Route to `graphql/{*path}` then you are done.
However, if you have changed either the default Azure Function prefix (which is `/api/`) or use a different Route binding, then you
need to explicitly tell the AzureFunctionsProxy what the expected base Url path is, so that the HC Middleware will successfully
match the path and serve all necessary resources.
   - This is done easily by setting the `AzureFunctionsRoutePath` option in the configuration as follows:
   - Assuming the following then the configuration would be as follows:
     - You have changed the default Azure Functions prefix from `api` to `my-api` in the `host.json` file.
     - You have added a version `v1` in front of the Route binding and renamed it so your Route binding is now: `Route = "v1/graphql-service/{*path}"
     - *NOTE: you MUST still use the wildcard path matching for Playground to function properly.*
```csharp
        //Finally Initialize AzureFunctions Executor Proxy here...
        services.AddAzureFunctionsGraphQL((options) =>
        {
            //When accessing the GraphQL via AzureFunctions this is the path that all Urls will be prefixed with
            //  as configured in the AzureFunction host.json combined with the HttpTrigger Route binding.
            options.AzureFunctionsRoutePath = "/api/v1/graphql-service";
        });
```

#### Additional Playground Usage Notes:
For Playground to function properly, with Azure Functions V2 using the proxy library, you 
will have to use Anonymous function security when deployed (for now?).  This is becuase the HC 
web app does not include the original querystring values for Function token ?code=123 when it 
makes requests for assets so they will fail with 401 Unauthorized.  Alternatively, a 
Chrome Plugin can be used to set the Token as a header value `x-functions-key`.  

*It works well with the ModHeader Chrome Extension.  However, to eliminate that dependency, I may 
look into making it easier to serve the Playground from an Anonymous Function (without 
exposing the actual data endpoitn) and/or creating  Bookmarklet/Favlet that does 
this without an extension in Chrome as time permits...*


## Disclaimers:
* PlayGround & Schema Download Functionality:
  - There is one key reason that Playground and Schema Download works -- because the DEFAULT values for the GraphQL Options are to Enable them!  
  - At this time the AzureFunctionsProxy can enablee/disable the middleware by either wiring up the Middleware or not.
  - But, the HC middleware checks for GraphQL configured options that are set at Configuration build time to see if these are enable/disabled, 
and that is stored on *Endpoint Metadata* that is not accessible (to my knowledge so far) in the Azure Function
  - This is because in Azure Functions (V2) we do not control the routing configuration at the same level of control that 
an Asp.Net Core application has.
  - However, since the HC defaults are to be `Enabled = true` then it's a non-issue!
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
