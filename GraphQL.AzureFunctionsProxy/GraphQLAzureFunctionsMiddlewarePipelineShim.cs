//using System;
//using System.Collections.Generic;
//using System.Text;
//using HotChocolate.AspNetCore;

//namespace GraphQL.AzureFunctionsProxy
//{
//    public class GraphQLAzureFunctionsMiddlewarePipelineShim
//    {
//        public void RegisterMiddleware(MiddlewareBase middleware)
//        {

//        }

//    }

//    public class MiddlewareShim
//    {
//        public MiddlewareBase Next { get; set; }

//        protected HttpMiddleware(HttpMiddleware next)
//        {
//            this.Next = next;
//        }

//        protected HttpMiddleware()
//        {

//        }

//        public abstract Task InvokeAsync(IHttpFunctionContext context);
//    }
//}
