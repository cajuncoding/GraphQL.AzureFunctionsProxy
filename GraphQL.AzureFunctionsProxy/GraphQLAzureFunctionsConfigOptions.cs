using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.AzureFunctionsProxy
{
    public class GraphQLAzureFunctionsConfigOptions
    {
        public string AzureFunctionsGraphQLRoutePath { get; set; } = "/api/graphql";
        public bool EnableSchemaDefinitionMiddleware { get; set; } = true;
        public bool EnablePlayground { get; set; } = true;
        public bool EnableGetRequestMiddleware { get; set; } = true;
    }
}
