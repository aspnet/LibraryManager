// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix
{
    public class DisplayError
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

        /// <summary>A URL pointing to documentation about the error.</summary>
        public string HelpLink => string.Format(Constants.ErrorCodeLink, ErrorCode.ToLowerInvariant());

        /// <summary>The line number containing the error.</summary>
        public int Line { get; set; } = 0;

        /// <summary>The column number containing the error.</summary>
        public int Column { get; set; } = 0;

        public ImageMoniker Moniker => KnownMonikers.StatusWarning;
    }
}
