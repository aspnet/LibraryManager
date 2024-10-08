﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Web.LibraryManager
{
    /// <summary>
    /// Library Manager constants
    /// </summary>
    public static class ManifestConstants
    {
        /// <summary>
        /// libman.json libraries element
        /// </summary>
        public const string Version = "version";

        /// <summary>
        /// libman.json libraries element
        /// </summary>
        public const string Libraries = "libraries";

        /// <summary>
        /// libman.json library element
        /// </summary>
        public const string Library = "library";

        /// <summary>
        /// libman.json destination element
        /// </summary>
        public const string Destination = "destination";

        /// <summary>
        /// libman.json defaultDestination element
        /// </summary>
        public const string DefaultDestination = "defaultDestination";

        /// <summary>
        /// libman.json provider element
        /// </summary>
        public const string Provider = "provider";

        /// <summary>
        /// libman.json defaultProvider element
        /// </summary>
        public const string DefaultProvider = "defaultProvider";

        /// <summary>
        /// libman.json files element
        /// </summary>
        public const string Files = "files";

        /// <summary>
        /// libman.json fileMappings element
        /// </summary>
        public const string FileMappings = "fileMappings";

        /// <summary>
        /// libman.json root element
        /// </summary>
        public const string Root = "root";

        /// <summary>
        /// For providers that support versioned libraries, this represents the evergreen latest version
        /// </summary>
        public const string LatestVersion = "latest";
    }
}
