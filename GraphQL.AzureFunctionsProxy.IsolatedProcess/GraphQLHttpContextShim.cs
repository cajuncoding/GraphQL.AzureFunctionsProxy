using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
//using QueryCollection = Microsoft.AspNetCore.Http.QueryCollection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.AzureFunctionsProxy
{
    public class GraphQLHttpContextShim : IDisposable, IAsyncDisposable
    {
        protected HttpContext HttpContextShim { get; set; }

        public HttpRequestData IsolatedProcessHttpRequestData { get; protected set; }

        public string ContentType { get; protected set; }

        public GraphQLHttpContextShim(HttpRequestData httpRequestData)
        {
            this.IsolatedProcessHttpRequestData = httpRequestData ?? throw new ArgumentNullException(nameof(httpRequestData));
            this.ContentType = httpRequestData.GetContentType();
        }

        /// <summary>
        /// Create an HttpContext (AspNetCore compatible) that can be provided to the AzureFunctionsProxy for GraphQL execution.
        /// All pertinent data from the HttpRequestData provided by the Azure Functions Isolated Process will be marshalled
        /// into the HttpContext for HotChocolate to consume.
        /// </summary>
        /// <returns></returns>
        public virtual async Task<HttpContext> CreateGraphQLHttpContextAsync()
        {
            var httpRequestData = this.IsolatedProcessHttpRequestData;

            var httpContextShim = BuildGraphQLHttpContext(
                requestHttpMethod: httpRequestData.Method,
                requestUri: httpRequestData.Url,
                requestHeadersCollection: httpRequestData.Headers,
                requestBody: await httpRequestData.ReadAsStringAsync().ConfigureAwait(false),
                requestBodyContentType: httpRequestData.GetContentType(),
                claimsIdentities: httpRequestData.Identities
            );

            //Ensure we track the HttpContext internally for cleanup when disposed!
            this.HttpContextShim = httpContextShim;
            return httpContextShim;
        }

        protected virtual HttpContext BuildGraphQLHttpContext(
            string requestHttpMethod,
            Uri requestUri,
            HttpHeadersCollection requestHeadersCollection,
            string requestBody = null,
            string requestBodyContentType = "application/json",
            IEnumerable<ClaimsIdentity> claimsIdentities = null
        )
        {
            //Initialize the root Http Context (Container)...
            var httpContext = new DefaultHttpContext();

            //Initialize the Http Request...
            var httpRequest = httpContext.Request;
            httpRequest.Scheme = requestUri.Scheme;
            httpRequest.Path = new PathString(requestUri.AbsolutePath);
            httpRequest.Method = requestHttpMethod ?? HttpMethod.Post.Method;
            httpRequest.QueryString = new QueryString(requestUri.Query);

            //Ensure we marshall across all Headers from teh Client Request...
            if (requestHeadersCollection?.Any() == true)
                foreach(var header in requestHeadersCollection)
                    httpRequest.Headers.Add(header.Key, new StringValues(header.Value.ToArray()));
            
            if (!string.IsNullOrEmpty(requestBody))
            {
                //Initialize a valid Stream for the Request (must be tracked & Disposed of!)
                var requestBodyBytes = Encoding.UTF8.GetBytes(requestBody);
                httpRequest.Body = new MemoryStream(requestBodyBytes);
                httpRequest.ContentType = requestBodyContentType;
                httpRequest.ContentLength = requestBodyBytes.Length;
            }

            //Initialize the Http Response...
            var httpResponse = httpContext.Response;
            //Initialize a valid Stream for the Response (must be tracked & Disposed of!)
            //NOTE: Default Body is a NullStream...which ignores all Reads/Writes.
            httpResponse.Body = new MemoryStream();

            //Proxy over any possible authentication claims if available
            if (claimsIdentities?.Any() == true)
            {
                httpContext.User = new ClaimsPrincipal(claimsIdentities);
            }

            return httpContext;
        }

        /// <summary>
        /// Create an HttpResponseData containing the proxied GraphQL results; marshalled back from
        /// the HttpContext that HotChocolate populates.
        /// </summary>
        /// <returns></returns>
        public async Task<HttpResponseData> CreateHttpResponseDataAsync()
        {
            var graphqlResponseBytes = ReadResponseBytes();
            var httpContext = this.HttpContextShim;
            var httpStatusCode = (HttpStatusCode)httpContext.Response.StatusCode;

            //Initialize the Http Response...
            var httpRequestData = this.IsolatedProcessHttpRequestData;
            var response = httpRequestData.CreateResponse(httpStatusCode);

            //Marshall over all Headers from the HttpContext...
            //Note: This should also handle Cookies (not tested)....
            var responseHeaders = httpContext.Response?.Headers;
            if (responseHeaders?.Any() == true)
                foreach (var header in responseHeaders)
                    response.Headers.Add(header.Key, header.Value.Select(sv => sv.ToString()));

            //Marshall the original response Bytes from HotChocolate...
            //Note: This enables full support for GraphQL Json results/errors, binary downloads, SDL, & BCP binary data.
            await response.WriteBytesAsync(graphqlResponseBytes).ConfigureAwait(false);

            return response;
        }

        public byte[] ReadResponseBytes()
        {
            if (HttpContextShim?.Response?.Body is not MemoryStream responseMemoryStream) 
                return null;
            
            var bytes = responseMemoryStream.ToArray();
            return bytes;

        }

        public virtual string ReadResponseContentAsString()
        {
            var responseContent = Encoding.UTF8.GetString(ReadResponseBytes());
            return responseContent;
        }

        public ValueTask DisposeAsync()
        {
            this.Dispose();
            return ValueTask.CompletedTask;
        }

        public virtual void Dispose()
        {
            DisposeHttpContext();
            GC.SuppressFinalize(this);
        }

        protected virtual void DisposeHttpContext()
        {
            var httpContext = this.HttpContextShim;
            httpContext?.Request?.Body?.Dispose();
            httpContext?.Response?.Body?.Dispose();

            this.HttpContextShim = null;
        }
    }
}
