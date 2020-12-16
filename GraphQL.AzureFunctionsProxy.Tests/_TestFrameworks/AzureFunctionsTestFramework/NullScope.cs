using System;

namespace Functions.Tests
{
    /// <summary>
    /// Borrowed form Microsoft Azure Functions test walk-through
    /// For more info see: https://docs.microsoft.com/en-us/azure/azure-functions/functions-test-a-function
    /// </summary>
    public class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new NullScope();

        private NullScope() { }

        public void Dispose() { }
    }
}