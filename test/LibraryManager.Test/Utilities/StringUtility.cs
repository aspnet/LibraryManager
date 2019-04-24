using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Web.LibraryManager.Test.Utilities
{
    internal static class StringUtility
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
    }
}
