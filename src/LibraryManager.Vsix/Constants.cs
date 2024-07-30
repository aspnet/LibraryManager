// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal static class Constants
    {
        public const string ConfigFileName = "libman.json";
        public const string TelemetryNamespace = "vs/webtools/librarymanager/";
        public const string MainNuGetPackageId = "Microsoft.Web.LibraryManager.Build";
        public const string ErrorCodeLink = "https://github.com/aspnet/LibraryManager/wiki/Error-codes#{0}";
        public const string WAP = "{349C5851-65DF-11DA-9384-00065B846F21}";
        public const string WebsiteProject = "{E24C65DC-7377-472B-9ABA-BC803B73C61A}";
        /// <summary>
        /// Project capability for .NET web projects. Used only for UI context rules.
        /// </summary>
        public const string DotNetCoreWebCapability = "DotNetCoreWeb";
        /// <summary>
        /// Project capability for CPS-based projects. Used to determine how to add/remove items to the project.
        /// </summary>
        public const string CpsCapability = "CPS";
    }
}
