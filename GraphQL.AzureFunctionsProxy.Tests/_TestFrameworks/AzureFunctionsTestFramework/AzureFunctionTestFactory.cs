using System;
using System.Collections.Generic;
using System.Text;
using Functions.Tests;
using HotChocolate.AzureFunctionsProxy;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StarWars.AzureFunctions;

namespace GraphQL.AzureFunctionsProxy.Tests.AzureFunctionsTestFramework
{
    public class AzureFunctionTestFactory
    {
        public static AzureFunctionTestContext CreateFunctionTestContext<TFunction>(FunctionsStartup functionStartupInstance)
            where TFunction: class
        {
            var host = new HostBuilder()
                .ConfigureWebJobs(builder =>
                {
                    functionStartupInstance.Configure(builder);

                    //Add the Function Type for easy Resolving of dependencies!
                    builder.Services.AddScoped<TFunction>();
                    
                })
                .Build();

            return new AzureFunctionTestContext(host);
        }

        public static ILogger CreateLogger(LoggerTypes type = LoggerTypes.List)
        {
            var logger = type == LoggerTypes.List 
                ? new ListLogger() 
                : NullLoggerFactory.Instance.CreateLogger("Null Logger");

            return logger;
        }
    }
}
