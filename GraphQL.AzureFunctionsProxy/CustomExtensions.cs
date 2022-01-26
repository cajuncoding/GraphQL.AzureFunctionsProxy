using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.AzureFunctionsProxy
{
    public static class ArrayCustomExtensions
    {
        public static T[] ForEach<T>(this T[] items, Action<T> action)
        {
            if (items == null) return null;

            for (var i = 0; i < items.Length; i++)
                action(items[i]);

            return items;
        }

        public static TResult[] ForEach<T, TResult>(this T[] items, Func<T, TResult> fn)
        {
            if (items == null) return null;
            
            var results = new TResult[items.Length];
            for (var i = 0; i < items.Length; i++)
                results[i] = fn(items[i]);

            return results;
        }

    }
}
