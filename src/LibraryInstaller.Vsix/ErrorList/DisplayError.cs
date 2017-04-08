// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using LibraryInstaller.Contracts;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace LibraryInstaller.Vsix
{
    public class DisplayError
    {
        public DisplayError(IError error)
        {
            ErrorCode = error.Code;
            Description = error.Message;
        }

        /// <summary>The error code is displayed in the Error List.</summary>
        public string ErrorCode { get; private set; }

        /// <summary>A short description of the error.</summary>
        public string Description { get; private set; }

        /// <summary>A URL pointing to documentation about the error.</summary>
        public string HelpLink => string.Format(Constants.ErrorCodeLink, ErrorCode.ToLowerInvariant());

        /// <summary>The line number containing the error.</summary>
        public int Line { get; private set; } = 0;

        /// <summary>The column number containing the error.</summary>
        public int Column { get; private set; } = 0;

        public ImageMoniker Moniker => KnownMonikers.StatusWarning;
    }
}
