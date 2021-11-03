using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace HotChocolate.AzureFunctionsProxy
{
    public static class HttpRequestDataExtensions
    {
        public static string GetContentType(this HttpRequestData httpRequestData, string defaultValue = "application/json")
        {
            var contentType = httpRequestData.Headers.TryGetValues(HeaderNames.ContentType, out var contentTypeHeaders)
                ? contentTypeHeaders.FirstOrDefault()
                : defaultValue;

            return contentType;
        }

        public static HttpResponseData CreateStringMessageResponse(this HttpRequestData httpRequestData, HttpStatusCode httpStatus, string message)
        {
            if (httpRequestData == null)
                throw new ArgumentNullException(nameof(httpRequestData));

            var response = httpRequestData.CreateResponse();
            response.StatusCode = httpStatus;
            response.WriteString(message);
            return response;
        }


        public static HttpResponseData CreateBadRequestErrorMessageResponse(this HttpRequestData httpRequestData, string message)
        {
            return httpRequestData.CreateStringMessageResponse(HttpStatusCode.BadRequest, message);
        }

        public static string GetQueryStringParam(this HttpRequestData httpRequestData, string queryParamName)
        {
            const string QueryStringItemsKey = "QueryStringParameters";
            var functionContextItems = httpRequestData.FunctionContext.Items;

            Dictionary<string, StringValues> queryStringParams;
            if (functionContextItems.TryGetValue(QueryStringItemsKey, out var existingQueryParams))
            {
                queryStringParams = existingQueryParams as Dictionary<string, StringValues>;
            }
            else
            {
                queryStringParams = QueryHelpers.ParseQuery(httpRequestData.Url.Query);
                httpRequestData.FunctionContext.Items.Add(QueryStringItemsKey, queryStringParams);
            }

            var queryValue = queryStringParams != null && queryStringParams.TryGetValue(queryParamName, out var stringValues)
                ? stringValues.FirstOrDefault()
                : null;

            return queryValue;
        }
    }
}
