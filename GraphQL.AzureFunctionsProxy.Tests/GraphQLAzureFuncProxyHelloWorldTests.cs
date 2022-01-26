using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.AzureFunctionsProxy.Tests.AzureFunctionsTestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StarWars.AzureFunctions;

namespace GraphQL.AzureFunctionsProxy.Tests
{
    [TestClass]
    public class GraphQLAzureFuncProxyHelloWorldTests : AzureFunctionGraphQLTestBase
    {
        [TestMethod]
        public async Task TestQueryNoSelections()
        {   //arrange
            using var functionTestCtx = AzureFunctionTestFactory.CreateFunctionTestContext<HelloWorldFunction>(new HelloWorldFunctionsStartup());

            var graphQLQuery = "{ hello }";
            var httpRequest = CreateGraphQLQueryRequest(functionTestCtx, graphQLQuery);

            var helloWorldFunction = functionTestCtx.ResolveFunctionWithDependencies<HelloWorldFunction>();

            //act
            var functionResult = await helloWorldFunction
                .Run(httpRequest, functionTestCtx.Logger, CancellationToken.None)
                .ConfigureAwait(false);
            var graphQLResult = functionTestCtx.ReadResponseJson<GraphQLQueryResult>();

            //assert
            Assert.IsNotNull(functionResult);
            Assert.IsNotNull(graphQLResult?.Data, "Query Execution Failed");

            var helloResult = graphQLResult.Data["hello"]?.ToString();
            Assert.AreEqual(helloResult, "Hello World!");
            WriteLine(helloResult);
        }

        [TestMethod]
        public async Task TestQueryNoSelectionsWithArgument()
        {   //arrange
            using var functionTestCtx = AzureFunctionTestFactory.CreateFunctionTestContext<HelloWorldFunction>(new HelloWorldFunctionsStartup());

            var graphQLQuery = @"{ hello(name: ""CajunCoding"") }";
            var httpRequest = CreateGraphQLQueryRequest(functionTestCtx, graphQLQuery);

            var helloWorldFunction = functionTestCtx.ResolveFunctionWithDependencies<HelloWorldFunction>();

            //act
            var functionResult = await helloWorldFunction
                .Run(httpRequest, functionTestCtx.Logger, CancellationToken.None)
                .ConfigureAwait(false);
            var graphQLResult = functionTestCtx.ReadResponseJson<GraphQLQueryResult>();

            //assert
            Assert.IsNotNull(functionResult);
            Assert.IsNotNull(graphQLResult?.Data, "Query Execution Failed");

            var helloResult = graphQLResult.Data["hello"]?.ToString();
            Assert.AreEqual(helloResult, "Hello CajunCoding!");
            WriteLine(helloResult);
        }

        [TestMethod]
        public async Task TestQueryThatGeneratesAnError()
        {   //arrange
            using var functionTestCtx = AzureFunctionTestFactory.CreateFunctionTestContext<HelloWorldFunction>(new HelloWorldFunctionsStartup());

            const string createErrorArg = "Create Error!";

            var graphQLQuery = $@"{{ hello(name: ""{createErrorArg}"") }}";
            var httpRequest = CreateGraphQLQueryRequest(functionTestCtx, graphQLQuery);

            var helloWorldFunction = functionTestCtx.ResolveFunctionWithDependencies<HelloWorldFunction>();

            //act
            var functionResult = await helloWorldFunction
                .Run(httpRequest, functionTestCtx.Logger, CancellationToken.None)
                .ConfigureAwait(false);
            var graphQLResult = functionTestCtx.ReadResponseJson<GraphQLQueryResult>();

            //assert
            Assert.IsNotNull(functionResult);
            Assert.IsNotNull(graphQLResult?.Data, "Query Execution Failed");
            Assert.IsNotNull(graphQLResult?.Errors, "Expected Errors but none were found!");

            var helloResult = graphQLResult.Data["hello"]?.ToString();
            Assert.AreEqual(helloResult, null);

            var errorResults = graphQLResult.Errors;
            Assert.AreEqual(errorResults.Count, 1);


            //BBernard - 01/26/2022
            //NOTE: Detailed exception information is only provided when in DEBUG exception as "extensions" data, but is null when Run (e.g. Release)
            //      therefore we only validate that an exception occurred as expected and we see the generic exception message from HC!
            var error = graphQLResult.Errors.First();
            var errorMessage = error["message"] as string;
            Assert.AreEqual(errorMessage, "Unexpected Execution Error");

            //WriteLine(errorMessage);

            WriteLine(JsonConvert.SerializeObject(graphQLResult, Formatting.Indented));
        }

        [TestMethod]
        public async Task TestQueryWithSelections()
        {   //arrange
            using var functionTestCtx = AzureFunctionTestFactory.CreateFunctionTestContext<HelloWorldFunction>(new HelloWorldFunctionsStartup());

            var graphQLQuery = @"{ 
                helloWorld {
                    language
                    translation
                }
            }";

            var httpRequest = CreateGraphQLQueryRequest(functionTestCtx, graphQLQuery);

            var helloWorldFunction = functionTestCtx.ResolveFunctionWithDependencies<HelloWorldFunction>();

            //act
            var functionResult = await helloWorldFunction
                .Run(httpRequest, functionTestCtx.Logger, CancellationToken.None)
                .ConfigureAwait(false);
            var graphQLResult = functionTestCtx.ReadResponseJson<GraphQLQueryResult>();

            //assert
            Assert.IsNotNull(functionResult);
            Assert.IsNotNull(graphQLResult?.Data, "Query Execution Failed");

            var json = (JToken)graphQLResult.Data["helloWorld"];
            
            Assert.AreEqual(
                json.FirstOrDefault(t => t["language"]?.ToString() == "English")?["translation"]?.ToString(),
                "Hello World"
            );

            Assert.AreEqual(
                json.FirstOrDefault(t => t["language"]?.ToString() == "Hawaiian")?["translation"]?.ToString(),
                "Aloha Honua"
            );

            WriteLine(json.ToString());
        }
    }
}
