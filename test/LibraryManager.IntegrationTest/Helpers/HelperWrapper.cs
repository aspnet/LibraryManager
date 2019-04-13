// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Web.LibraryManager.IntegrationTest.Helpers
{
    public class HelperWrapper
    {
        public HelperWrapper()
        {
            Completion = new CompletionHelper();
            FileIO = new FileIOHelper();
        }

        public CompletionHelper Completion { get; private set; }

        public FileIOHelper FileIO { get; private set; }

    }
}
