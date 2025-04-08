﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Test.Apex.VisualStudio;

namespace Microsoft.Web.LibraryManager.IntegrationTest.Helpers
{
    public class HelperWrapper
    {
        public HelperWrapper(VisualStudioHost vsHost)
        {
            Completion = new CompletionHelper();
            FileIO = new FileIOHelper();
        }

        public CompletionHelper Completion { get; private set; }

        public FileIOHelper FileIO { get; private set; }

    }
}
