// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        private Dictionary<string, string> _arrangedResponses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns a Stream for testing purposes
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<Stream> GetStreamAsync(string url, CancellationToken cancellationToken)
        {
            UnicodeEncoding uniEncoding = new UnicodeEncoding();
            string responseText = "Stream content for test";
            if (_arrangedResponses.ContainsKey(url))
            {
                responseText = _arrangedResponses[url];
            }

            byte[] content = Encoding.Default.GetBytes(responseText);

            return Task.FromResult<Stream>( new MemoryStream(content));
        }

        /// <summary>
        /// Register a prerecorded response for a specified URL
        /// </summary>
        /// <param name="url"></param>
        /// <param name="responseContent"></param>
        /// <returns></returns>
        public WebRequestHandler ArrangeResponse(string url, string responseContent)
        {
            _arrangedResponses.Add(url, responseContent);

            return this;
        }
    }
}
