// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        public string GetUserInputWithDefault(string fieldName, string defaultValue)
        {
            if (Inputs.TryGetValue(fieldName, out string value))
            {
                return value;
            }

            return defaultValue;
        }
    }
}
