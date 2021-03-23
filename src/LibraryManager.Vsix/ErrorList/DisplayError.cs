// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix.ErrorList
{
    internal class DisplayError
    {
        public DisplayError(IError error)
        {
            ErrorCode = error.Code;
            Description = error.Message;
        }

        /// <summary>The error code is displayed in the Error List.</summary>
        public string ErrorCode { get; }

        /// <summary>A short description of the error.</summary>
        public string Description { get; }

#pragma warning disable CA1308 // Normalize strings to uppercase
                               // Reason: we prefer the URLs to be lowercase.
        /// <summary>A URL pointing to documentation about the error.</summary>
        public string HelpLink => string.Format(Constants.ErrorCodeLink, ErrorCode.ToLowerInvariant());
#pragma warning restore CA1308 // Normalize strings to uppercase

        /// <summary>The line number containing the error.</summary>
        public int Line { get; set; }

        /// <summary>The column number containing the error.</summary>
        public int Column { get; set; }

        public ImageMoniker Moniker => KnownMonikers.StatusWarning;
    }
}
