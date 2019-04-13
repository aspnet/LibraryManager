// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    internal class NpmPackageInfo
    {
        internal string Author { get; private set; }
        internal string Description { get; private set; }
        internal string LatestVersion { get; private set; }
        internal string Homepage { get; private set; }
        internal string License { get; private set; }
        internal string Name { get; private set; }
        internal IList<SemanticVersion> Versions { get; private set; }

        internal NpmPackageInfo(string name, string description, string latestVersion, string author, string homepage, string license)
        {
            Name = name;
            Description = description;
            LatestVersion = latestVersion;
            Author = author;
            Homepage = homepage;
            License = license;
        }

        internal NpmPackageInfo(string name, string description, string latestVersion, string author, string homepage, string license, IList<SemanticVersion> versions)
        {
            Name = name;
            Description = description;
            LatestVersion = latestVersion;
            Author = author;
            Homepage = homepage;
            License = license;
            Versions = versions;
        }

        internal static NpmPackageInfo Parse(JObject packageInfo)
        {
            string name = packageInfo.GetJObjectMemberStringValue("name");
            string description = packageInfo.GetJObjectMemberStringValue("description");
            string version = packageInfo.GetJObjectMemberStringValue("version");
            string homepage = packageInfo.GetJObjectMemberStringValue("homepage");
            string license = packageInfo.GetJObjectMemberStringValue("license");

            string author = string.Empty;
            if (packageInfo["author"] is JObject authorObject)
            {
                author = authorObject.GetJObjectMemberStringValue("name");
            }

            return new NpmPackageInfo(name, description, version, author, homepage, license);
        }
    }
}
