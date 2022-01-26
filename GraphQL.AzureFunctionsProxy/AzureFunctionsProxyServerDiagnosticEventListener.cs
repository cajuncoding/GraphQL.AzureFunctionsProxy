using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AzureFunctionsProxy
{
    public class AzureFunctionsProxyServerDiagnosticEventListener : IServerDiagnosticEventListener
    {
        public static readonly IServerDiagnosticEventListener NoopDiagnosticEventListener = new AzureFunctionsProxyServerDiagnosticEventListener();

        private readonly IServerDiagnosticEventListener[] _listeners;

        /// <summary>
        /// Listeners collection is optional; if not defined this class will do nothing providing a No-op default implementation.
        /// </summary>
        /// <param name="listeners"></param>
        public AzureFunctionsProxyServerDiagnosticEventListener(IServerDiagnosticEventListener[] listeners = null)
        {
            _listeners = listeners;
        }

        public IDisposable ExecuteHttpRequest(HttpContext context, HttpRequestKind kind)
        {
            var disposableResults = _listeners.ForEach(l => l.ExecuteHttpRequest(context, kind));
            return new AggregateDisposable(disposableResults);
        }

        public void StartSingleRequest(HttpContext context, GraphQLRequest request)
            => _listeners?.ForEach(l => l.StartSingleRequest(context, request));

        public void StartBatchRequest(HttpContext context, IReadOnlyList<GraphQLRequest> batch)
            => _listeners?.ForEach(l => l.StartBatchRequest(context, batch));

        public void StartOperationBatchRequest(HttpContext context, GraphQLRequest request, IReadOnlyList<string> operations)
            => _listeners?.ForEach(l => l.StartOperationBatchRequest(context, request, operations));

        public void HttpRequestError(HttpContext context, IError error)
            => _listeners?.ForEach(l => l.HttpRequestError(context, error));

        public void HttpRequestError(HttpContext context, Exception exception)
            => _listeners?.ForEach(l => l.HttpRequestError(context, exception));

        public IDisposable ParseHttpRequest(HttpContext context)
        {
            var disposableResults = _listeners?.ForEach(l => l.ParseHttpRequest(context));
            return new AggregateDisposable(disposableResults);
        }

        public void ParserErrors(HttpContext context, IReadOnlyList<IError> errors)
            => _listeners?.ForEach(l => l.ParserErrors(context, errors));

        public IDisposable FormatHttpResponse(HttpContext context, IQueryResult result)
        {
            var disposableResults = _listeners?.ForEach(l => l.FormatHttpResponse(context, result));
            return new AggregateDisposable(disposableResults);
        }

        public IDisposable WebSocketSession(HttpContext context)
        {
            var disposableResults = _listeners?.ForEach(l => l.WebSocketSession(context));
            return new AggregateDisposable(disposableResults);
        }

        public void WebSocketSessionError(HttpContext context, Exception exception)
            => _listeners?.ForEach(l => l.WebSocketSessionError(context, exception));
    }

    /// <summary>
    /// DisposableItems collection is optional; if not defined this class will do nothing providing a No-op default implementation.
    /// </summary>
    public class AggregateDisposable : IDisposable
    {
        private readonly IDisposable[] _disposableItems;
        private bool _disposed;

        public AggregateDisposable(IDisposable[] disposableItems = null)
        {
            _disposableItems = disposableItems;
            _disposed = disposableItems == null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposableItems?.ForEach(i => i.Dispose());
            _disposed = true;
        }
    }
}
