// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.Web.LibraryManager.Utilities
{
    /// <summary>
    /// Utility for path template operations.
    /// </summary>
    public static class PathTemplateUtility
    {
        /// <summary>
        /// Expands a path template using [Name] and [Version] tokens.
        /// </summary>
        /// <param name="template">Template string</param>
        /// <param name="name">Library name</param>
        /// <param name="version">Library version</param>
        [SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "Not available on net481, not needed here (caseless)")]
        public static string ExpandPathTemplate(string template, string name, string version)
        {
            if (template is null || !template.Contains('['))
            {
                return template;
            }

            // if the name contains a slash (either filesystem or scoped packages),
            // trim that and only take the last segment as the library name.
            int cutIndex = name.LastIndexOfAny(['/', '\\']);
            name = cutIndex == -1 ? name : name.Substring(cutIndex + 1);

            return template.Replace("[Name]", name)
                           .Replace("[Version]", version);
        }
    }
}
