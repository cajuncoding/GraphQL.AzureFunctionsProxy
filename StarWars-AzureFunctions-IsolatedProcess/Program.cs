using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using HotChocolate.AzureFunctionsProxy;
using Microsoft.Extensions.DependencyInjection;
using StarWars.Common;

namespace StarWars_AzureFunctions_OutOfProcessProcess2
{
    public class Program
    {
        public static Task Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(services =>
                {
                    services
                        .AddHttpContextAccessor()
                        .AddStarWarsServices();

                    services
                        .AddGraphQLServer()
                        .ConfigureStarWarsGraphQLServer();

                    //Finally Initialize AzureFunctions Executor Proxy here...
                    services.AddAzureFunctionsGraphQL((options) =>
                    {
                        //The Path must match the exact routing path that the Azure Function HttpTrigger is bound to.
                        //NOTE: ThIs includes the /api/ prefix unless it was specifically removed or changed in the host.json file.
                        //NOTE: THe default value is `/api/graphql`, but it's being done here to illustrate how to set the value.
                        options.AzureFunctionsRoutePath = "/api/graphql/bcp";
                    });

                })
                .Build();

            return host.RunAsync();
        }
    }
}