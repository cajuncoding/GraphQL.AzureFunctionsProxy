using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.AzureFunctionsProxy
{
    public class GraphQLAzureFunctionsConfigOptions
    {
        public string AzureFunctionsRoutePath { get; set; } = "/api/graphql";
        public bool EnableSchemaDefinitionDownload { get; set; } = true;
        public bool EnablePlaygroundWebApp { get; set; } = true;
        public bool EnableGETRequests { get; set; } = true;
    }
}
