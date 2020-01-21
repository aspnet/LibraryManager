// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts.Caching;

namespace Microsoft.Web.LibraryManager.Mocks.CacheServices
{
    /// <summary>
    /// Fake ICacheService for Cdnjs related unit tests
    /// </summary>
    public class FakeCdnjsCacheService : ICacheService
    {
        Task<string> ICacheService.GetCatalogAsync(string url, string cacheFile, CancellationToken cancellationToken)
        {
            return Task.FromResult(@"{
    ""results"": [
        {
                ""name"": ""sampleLibrary"",
            ""latest"": ""https://test-library.com/sample/js/sampleLibrary.min.js"",
            ""description"": ""A sample library for testing"",
            ""version"": ""3.1.4""
        },
        {
                ""name"": ""test-library"",
            ""latest"": ""https://test-library.com/test-library.min.js"",
            ""description"": ""A fake library for testing"",
            ""version"": ""1.0.0""
        },
        {
                ""name"": ""test-library2"",
            ""latest"": ""https://test-library.com/test-library2.min.js"",
            ""description"": ""A second fake library for testing"",
            ""version"": ""2.0.0""
        }
    ],
    ""total"": 3
}");
        }

        Task<string> ICacheService.GetMetadataAsync(string url, string cacheFile, CancellationToken cancellationToken)
        {
            return Task.FromResult(@"{
    ""name"": ""sampleLibrary"",
    ""filename"": ""sample/js/sampleLibrary.min.js"",
    ""version"": ""3.1.4"",
    ""description"": ""Sample library for test input"",
    ""assets"": [
        {
                ""version"": ""4.0.0-beta.1"",
            ""files"": [
                ""sample/js/sampleLibrary.js"",
                ""sample/js/sampleLibrary.min.js"",
                ""sample/betaFile.js""
            ]
    },
        {
            ""version"": ""4.0.0-beta.2"",
            ""files"": [
                ""sample/js/sampleLibrary.js"",
                ""sample/js/sampleLibrary.min.js"",
                ""sample/betaFile.js""
            ]
},
        {
            ""version"": ""4.0.0-beta.10"",
            ""files"": [
                ""sample/js/sampleLibrary.js"",
                ""sample/js/sampleLibrary.min.js"",
                ""sample/betaFile.js""
            ]
        },
        {
            ""version"": ""3.1.4"",
            ""files"": [
                ""sample/js/sampleLibrary.js"",
                ""sample/js/sampleLibrary.min.js""
            ]
        },
        {
            ""version"": ""2.0.0"",
            ""files"": [
                ""sample/js/sampleLibrary.js"",
                ""sample/js/sampleLibrary.min.js"",
                ""sample/outdatedFile.js""
            ]
        }
    ]
}");
        }
    }
}
