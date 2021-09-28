using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.AzureFunctionsProxy.Tests.AzureFunctionsTestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using StarWars.AzureFunctions;

namespace GraphQL.AzureFunctionsProxy.Tests
{
    [TestClass]
    public class GraphQLSdlAndBananaCakePopTests : AzureFunctionGraphQLTestBase
    {
        [TestMethod]
        public async Task TestSDLRetrieval()
        {   //arrange
            using var functionTestCtx = AzureFunctionTestFactory.CreateFunctionTestContext<HelloWorldFunction>(new HelloWorldFunctionsStartup());

            var queryStringValues = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("SDL", "")
            };

            var httpRequest = functionTestCtx.CreateHttpGetRequest("", "", queryStringValues);

            var helloWorldFunction = functionTestCtx.ResolveFunctionWithDependencies<HelloWorldFunction>();

            //act
            var functionResult = await helloWorldFunction
                .Run(httpRequest, functionTestCtx.Logger, CancellationToken.None)
                .ConfigureAwait(false);
            
            var sdlResult = functionTestCtx.ReadResponseContentAsString();

            //assert
            Assert.AreEqual(functionTestCtx.HttpContext.Response.StatusCode, 200);
            Assert.IsNotNull(functionResult);
            Assert.IsNotNull(sdlResult, "Query Execution Failed");
            Assert.IsFalse(string.IsNullOrWhiteSpace(sdlResult));

            Assert.IsTrue(sdlResult.Contains("﻿schema {\r\n  query: Query\r\n}"));
            Assert.IsTrue(sdlResult.Contains("type HelloWorldTranslation {\r\n  language: String\r\n  translation: String\r\n}"));
        }

        [TestMethod]
        public async Task TestStaticFileRetrievalForManifestJson()
        {   //arrange
            using var functionTestCtx = AzureFunctionTestFactory.CreateFunctionTestContext<HelloWorldFunction>(new HelloWorldFunctionsStartup());

            var httpRequest = functionTestCtx.CreateHttpGetRequest("/api/graphql/manifest.json", "application/json");

            var helloWorldFunction = functionTestCtx.ResolveFunctionWithDependencies<HelloWorldFunction>();

            //act
            var functionResult = await helloWorldFunction
                .Run(httpRequest, functionTestCtx.Logger, CancellationToken.None)
                .ConfigureAwait(false);

            var responseBody = functionTestCtx.ReadResponseContentAsString();

            //assert
            Assert.AreEqual(200, functionTestCtx.HttpContext.Response.StatusCode);
            Assert.IsNotNull(functionResult);
            Assert.IsNotNull(responseBody, "Query Execution Failed");
            Assert.IsFalse(string.IsNullOrWhiteSpace(responseBody));

            Assert.IsTrue(responseBody.Contains("\"short_name\": \"Banana Cake Pop\""));
            Assert.IsTrue(responseBody.Contains("\"name\": \"Banana Cake Pop\""));
        }
    }
}
