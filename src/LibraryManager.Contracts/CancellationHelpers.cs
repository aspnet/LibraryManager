// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Contracts
{
    internal static class CancellationHelpers
    {
        public static Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var src = new TaskCompletionSource<T>();
            cancellationToken.Register(() => src.SetCanceled());
            return Task.WhenAny(task, src.Task).Unwrap();
        }
    }
}
