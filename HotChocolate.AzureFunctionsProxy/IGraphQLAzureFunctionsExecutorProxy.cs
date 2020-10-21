using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AzureFunctionsProxy
{
    public interface IGraphQLAzureFunctionsExecutorProxy
    {
        Task<IActionResult> ExecuteFunctionsQueryAsync(HttpContext context, ILogger log, CancellationToken cancellationToken);
    }
}