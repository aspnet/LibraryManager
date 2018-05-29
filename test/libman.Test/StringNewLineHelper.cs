// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Web.LibraryManager.Tools.Test
{
    internal static class StringHelper
    {
        public static string NormalizeNewLines(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            s = s.Replace("\r\n", "$$$").Replace("\r", "$$$").Replace("\n", "$$$");
            return s.Replace("$$$", Environment.NewLine);
        }

        public static bool AreEqualIgnoringNewLineFormats(string s1, string s2)
        {
            s1 = NormalizeNewLines(s1);
            s2 = NormalizeNewLines(s2);

            return s1 == s2;
        }
    }
}
