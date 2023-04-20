# GraphQL.AzureFunctionsProxy to HC v13 Migration Guide

Azure Functions are now an OOTB feature of HotChocolate GraphQL v13 supporting both In-process & Isolated Process models... 🙌🙌🙌

As of the new Hot Chocolate GraphQL Server v13 release, the Azure Functions Proxy and it's feature set are now an OOTB feature of Hot Chococlate -- along with some optimizations since it's part of the core code base! This was announced and shared in the [v13 introduction blog post here!](https://chillicream.com/blog/2023/02/08/new-in-hot-chocolate-13#azure-functions)

I collaborated closely with the core HC team to merge the valuable elements of the `GraphQL.AzureFunctionsProxy` library to enable support for both In-Process and Isolated Process Azure Functions as an OOTB feature of HC now! And therefore as of v13, this project will no longer be maintained as a separate library -- my efforts will be focused on maintaining the core functionality going forward with the HC team!

## Migrating an In-process Azure Function:

### v13 Configuration in your FunctionsStartup class:
```csharp
[assembly: FunctionsStartup(typeof(Startup))]
public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder
            .AddGraphQLFunction()
            //Optionally customize how the Azure Functions specific functionality works 
            //  (e.g. Enable/Disable the BananaCakePop IDE)...
            .ModifyFunctionOptions(options => options.Tool.Enable = true)
            //All of the following is normal HC Configuration
            .AddQueryType<Query>();
    }
}
```

### v13 Azure Function Binding:

Using the Dependency Injection approach...
*Note: this is the universal approach and is virtually identical to the Isolated process function binding below.*

TL;DR:
 - `IGraphQLAzureFunctionsExecutorProxy` now becomes `IGraphQLRequestExecutor`
 - The call to `ExecuteFunctionsQueryAsync` is now simplified as a call to `ExecuteAsync(request)`
```csharp
public class GraphQLFunction
{
    private readonly IGraphQLRequestExecutor _graphqlExecutor;
    public GraphQLFunction(IGraphQLRequestExecutor executor)
    {
        _graphqlExecutor = executor;
    }

    [FunctionName("GraphQLHttpFunction")]
    public Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "graphql/{**slug}")] HttpRequest request
    ) => _graphqlExecutor.ExecuteAsync(request);
}
```

Or if you really want to streamline you can use the custom binding; *but it's only supported on the In-Process model*:
```csharp
public class GraphQLFunction
{
    [FunctionName("GraphQLHttpFunction")]
    public Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "graphql/{**slug}")] HttpRequest request,
        [GraphQL] IGraphQLRequestExecutor executor
    ) => executor.ExecuteAsync(request);
}
```

## Migrating an Isolated Process Azure Function:

###v13 Startup configuration in you Program.cs class:
```csharp
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddGraphQLFunction(b => b.AddQueryType<Query>())
    .Build();

host.Run();
```

### v13 Azure Function Binding:

*Note: It's virtually identical to the above In-Process if you are using hte Dependency injection approach.*

TL;DR:
 - `IGraphQLAzureFunctionsExecutorProxy` now becomes `IGraphQLRequestExecutor`
 - The call to `ExecuteFunctionsQueryAsync` is now simplified as a call to `ExecuteAsync(request)`
```csharp
public class GraphQLFunction
{
    private readonly IGraphQLRequestExecutor _graphqlExecutor;
    public GraphQLFunction(IGraphQLRequestExecutor executor)
    {
        _graphqlExecutor = executor;
    }

    [Function("GraphQLHttpFunction")]
    public Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "graphql/{**slug}")] HttpRequestData request
    ) => _executor.ExecuteAsync(request);
}
```