// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Web.LibraryManager.Tools.Test
{
    internal class TestInputReader : IInputReader
    {
        public Dictionary<string, string> Inputs { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

        public string GetUserInput(string fieldName)
        {
            return Inputs[fieldName];
        }
    }
}
