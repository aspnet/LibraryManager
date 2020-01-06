// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Utilities;

namespace Microsoft.Web.LibraryManager.Test.Utilities
{
    [TestClass]
    public class ParallelUtilityTests
    {
        [TestMethod]
        public async Task ForEachAsync_NullAction_ShouldThrow()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await ParallelUtility.ForEachAsync(null, 1, Array.Empty<object>())
            );
        }

        [TestMethod]
        public async Task ForEachAsync_0DegreesParallelism_ShouldThrow()
        {
            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(async () =>
                await ParallelUtility.ForEachAsync((i) => Task.CompletedTask, 0, Array.Empty<object>())
            );
        }

        [TestMethod]
        public async Task ForEachAsync_NegativeDegreesParallelism_ShouldThrow()
        {
            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(async () =>
                await ParallelUtility.ForEachAsync((i) => Task.CompletedTask, -5, Array.Empty<object>())
            );
        }

        [TestMethod]
        public async Task ForEachAsync_NullItems_ShouldThrow()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await ParallelUtility.ForEachAsync((i) => Task.CompletedTask, 1, (object[])null)
            );
        }

        [TestMethod]
        public async Task ForEachAsync_ValidEmptyArguments_ShouldRunWithoutExceptions()
        {
            await ParallelUtility.ForEachAsync((i) => Task.CompletedTask, 1, Array.Empty<object>());
        }

        [TestMethod]
        public async Task ForEachAsync_IfOneTaskFails_AllTasksGetRun()
        {
            int counter = 0;

            Exception exception = await Assert.ThrowsExceptionAsync<Exception>(async () =>
                await ParallelUtility.ForEachAsync((i) =>
                {
                    if (i % 2 == 0)
                    {
                        throw new Exception("Expected Failure");
                    }
                    counter++;
                    return Task.CompletedTask;
                }, 1, Enumerable.Range(1, 3))
            );

            Assert.AreEqual("Expected Failure", exception.Message);
            Assert.AreEqual(2, counter); // both 1 and 3 ran, 2 threw the exception
        }

        [TestMethod]
        public async Task ForEachAsync_CancellationTokenSet_DoesNotRunAllTasks()
        {
            int counter = 0;
            var cts = new CancellationTokenSource();

            Exception exception = await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
                await ParallelUtility.ForEachAsync((i) =>
                {
                    if (i % 2 == 0)
                    {
                        cts.Cancel();
                    }
                    counter++;
                    return Task.CompletedTask;
                }, 1, Enumerable.Range(1, 3), cts.Token)
            );

            Assert.AreEqual(2, counter); // both 1 and 2 ran, but operation was cancelled before 3
        }

        [TestMethod]
        public async Task ForEachAsync_IfMultipleTasksFail_AllTasksGetRun_OnlyFirstExceptionIsReported()
        {
            int counter = 0;

            Exception exception = await Assert.ThrowsExceptionAsync<Exception>(async () =>
                await ParallelUtility.ForEachAsync((i) =>
                {
                    if (i % 2 == 0)
                    {
                        throw new Exception($"Expected Failure (iteration {i})");
                    }
                    counter++;
                    return Task.CompletedTask;
                }, 1, Enumerable.Range(1, 5))
            );

            Assert.AreEqual("Expected Failure (iteration 2)", exception.Message);
            Assert.AreEqual(3, counter); // both 1, 3, and 5 ran, 2 and 4 threw the exception
        }
    }
}
