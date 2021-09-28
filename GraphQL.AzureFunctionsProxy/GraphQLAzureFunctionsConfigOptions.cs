using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.AzureFunctionsProxy
{
    public class GraphQLAzureFunctionsConfigOptions
    {
        public string AzureFunctionsRoutePath { get; set; } = "/api/graphql";
        public bool EnableSchemaDefinitionDownload { get; set; } = true;

        [Obsolete("Use EnableBananaCakePop property instead; this will be removed in future updates.")]
        public bool EnablePlaygroundWebApp
        {
            get => EnableBananaCakePop;
            set => EnableBananaCakePop = value;
        }

        public bool EnableBananaCakePop { get; set; } = true;

        public bool EnableGETRequests { get; set; } = true;
    }
}
