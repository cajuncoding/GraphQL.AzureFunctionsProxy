using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using GraphQL.AzureFunctionsProxy.Tests.AzureFunctionsTestFramework;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StarWars.AzureFunctions;

namespace GraphQL.AzureFunctionsProxy.Tests
{
    public class AzureFunctionGraphQLTestBase
    {
        public TestContext TestContext { get; set; }

        public HttpRequest CreateGraphQLQueryRequest(AzureFunctionTestContext functionTestCtx, string query)
        {
            var graphQLRequest = new GraphQLQueryRequest { Query = query };
            var httpRequest = functionTestCtx.CreateHttpJsonPostRequest(graphQLRequest);

            return httpRequest;
        }


        public void WriteLine(string text)
        {
            TestContext?.WriteLine(text);
        }
    }
}
