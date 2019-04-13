// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Mocks
{
    /// <summary>
    /// Public Mock class for unit testing 
    /// </summary>
    public class WebRequestHandler : IWebRequestHandler
    {
        /// <summary>
        /// Returns a Stream for testing purposes
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<Stream> GetStreamAsync(string url, CancellationToken cancellationToken)
        {
            byte[] content = Encoding.Unicode.GetBytes("Stream content for test");

            return Task.FromResult<Stream>(new MemoryStream(content));
        }
    }
}
