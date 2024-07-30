// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
