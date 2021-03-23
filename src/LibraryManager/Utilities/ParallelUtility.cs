// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Utilities
{
    internal static class ParallelUtility
    {
        /// <summary>
        /// Executes an action over each element of a collection, up to the specified degree of parallelism.
        /// </summary>
        /// <param name="action">The action to be applied to each element</param>
        /// <param name="degreeOfParallelism">How many tasks to run in parallel</param>
        /// <param name="items">The items to operate over</param>
        public static Task ForEachAsync<T>(Func<T, Task> action, int degreeOfParallelism, IEnumerable<T> items)
        {
            return ForEachAsync<T>(action, degreeOfParallelism, items, CancellationToken.None);
        }

        /// <summary>
        /// Executes an action over each element of a collection, up to the specified degree of parallelism.
        /// </summary>
        /// <param name="action">The action to be applied to each element</param>
        /// <param name="degreeOfParallelism">How many tasks to run in parallel</param>
        /// <param name="items">The items to operate over</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static async Task ForEachAsync<T>(Func<T, Task> action, int degreeOfParallelism, IEnumerable<T> items, CancellationToken cancellationToken)
        {
            action = action ?? throw new ArgumentNullException(nameof(action));
            items = items ?? throw new ArgumentNullException(nameof(items));
            cancellationToken.ThrowIfCancellationRequested();

            using (var semaphore = new SemaphoreSlim(degreeOfParallelism, degreeOfParallelism))
            {
                var allTasks = new List<Task>();

                foreach (T item in items)
                {
                    await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();

                    Task task = DoActionAndRelease(action, item, semaphore);
                    allTasks.Add(task);
                }

                await Task.WhenAll(allTasks).ConfigureAwait(false);
            }
        }

        private static async Task DoActionAndRelease<T>(Func<T, Task> act, T input, SemaphoreSlim s)
        {
            try
            {
                await act(input).ConfigureAwait(false);
            }
            finally
            {
                s.Release();
            }
        }
    }
}
