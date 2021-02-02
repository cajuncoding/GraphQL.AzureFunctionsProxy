using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.AspNetCore;
using Microsoft.Extensions.FileProviders;

namespace HotChocolate.AzureFunctionsProxy
{
    public class GraphQLInitHelpers
    {
        /// <summary>
        /// The IFileProvider implementation from HC Core that serves static file resources dynamically.
        /// This was borrowed from HC Core 'static class EndpointRouteBuilderExtensions', but since it's private
        /// we just used similarly inspired logic here (not identical though as we use a publicly available type
        /// to get the HotChocolate.AspNetCore Assembly reference safely!
        /// </summary>
        /// <returns></returns>
        public static IFileProvider CreateEmbeddedFileProvider()
        {
            //NOTE: Use Publicly accessible 'GraphQLServerOptions' class to safely resolve the `HotChocolate.AspNetCore` Assembly!
            var type = typeof(GraphQLServerOptions);
            var resourceNamespace = typeof(MiddlewareBase).Namespace + ".Resources";

            return new EmbeddedFileProvider(type.Assembly, resourceNamespace);
        }
    }
}
