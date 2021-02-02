using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Functions.Tests;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace GraphQL.AzureFunctionsProxy.Tests.AzureFunctionsTestFramework
{
    public class AzureFunctionTestContext : IDisposable
    {
        public IHost WebJobsHost { get; protected set; }

        public ILogger Logger { get; protected set; }

        public HttpContext HttpContext { get; set; }

        public AzureFunctionTestContext(IHost webJobsHost, ILogger logger = null)
        {
            WebJobsHost = webJobsHost;
            Logger = logger ?? AzureFunctionTestFactory.CreateLogger(LoggerTypes.List);
        }

        public TFunction ResolveFunctionWithDependencies<TFunction>()
        {
            var function = WebJobsHost.Services.GetService<TFunction>();
            return function;
        }

        public HttpRequest CreateHttpGetRequest(string path, string contentType, List<KeyValuePair<string, string>> queryStringValues = null)
        {
            var httpRequest = CreateHttpRequest(
                HttpMethod.Get, 
                requestPath: path, 
                requestContentType: contentType, 
                queryStringValues: queryStringValues
            );
            return httpRequest;
        }

        public HttpRequest CreateHttpJsonPostRequest<TRequestPayload>(
            TRequestPayload requestPayload
        )
        {
            var requestBody = JsonConvert.SerializeObject(requestPayload);
            var httpRequest = CreateHttpRequest(HttpMethod.Post, requestBody: requestBody, requestContentType: "application/json");
            return httpRequest;
        }

        public HttpRequest CreateHttpRequest(
            HttpMethod httpMethod = null,
            string requestBody = null,
            string requestPath = "",
            string requestContentType = "application/json",
            List<KeyValuePair<string, string>> queryStringValues = null
        )
        {
            //Clean up any Streams that may already exist... so we can re-initialize...
            DisposeHttpContext();

            //Create the HttpContext
            this.HttpContext = new DefaultHttpContext();

            //Initialize the Http Request
            //NOTE: Default Body is a NullStream...which ignores all Reads/Writes.
            var httpRequest = this.HttpContext.Request;
            httpRequest.Path = new PathString(requestPath);
            httpRequest.Method = httpMethod?.Method ?? HttpMethod.Get.Method;
            httpRequest.Query = CreateQueryCollection(queryStringValues);

            //Initialize the Http Response
            var httpResponse = this.HttpContext.Response;
            //Initialize a valid Stream for the Response (must be tracked & Disposed of!)
            httpResponse.Body = new MemoryStream();

            if (requestBody != null)
            {
                //Initialize a valid Stream for the Request (must be tracked & Disposed of!)
                var requestBodyBytes = Encoding.UTF8.GetBytes(requestBody);
                httpRequest.Body = new MemoryStream(requestBodyBytes);
                httpRequest.ContentType = requestContentType;
                httpRequest.ContentLength = requestBodyBytes.Length;
            }

            return httpRequest;
        }

        public TResponsePayload ReadResponseJson<TResponsePayload>()
        {
            var responseContent = ReadResponseContentAsString();
            if (responseContent == null)
                return default;

            return JsonConvert.DeserializeObject<TResponsePayload>(responseContent);
        }

        public string ReadResponseContentAsString()
        {
            if (!(HttpContext?.Response?.Body is MemoryStream responseMemoryStream)) 
                return null;

            var responseContent = Encoding.UTF8.GetString(responseMemoryStream.ToArray());
            return responseContent;
        }

        protected QueryCollection CreateQueryCollection(List<KeyValuePair<string, string>> queryStringValues)
        {
            if (queryStringValues != null)
            {
                var queryGroups = queryStringValues.GroupBy(
                    kv => kv.Key,
                    kv => kv.Value
                ).ToDictionary(
                    g => g.Key,
                    g => new StringValues(g.ToArray())
                );

                return new QueryCollection(queryGroups);
            }

            return null;
        }

        protected void DisposeHttpContext()
        {
            var httpContext = this.HttpContext;
            httpContext?.Request?.Body?.Dispose();
            httpContext?.Response?.Body?.Dispose();

            this.HttpContext = null;
        }


        public void Dispose()
        {
            DisposeHttpContext();
            WebJobsHost?.Dispose();
        }
    }
}
