// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Helpers
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
