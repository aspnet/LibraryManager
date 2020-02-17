using System.Collections.Generic;
using Microsoft.Web.LibraryManager.Helpers;
using Newtonsoft.Json.Linq;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    /// <summary>
    /// Encapsulates information for an NPM package
    /// </summary>
    public sealed class NpmPackageInfo
    {
        /// <summary>
        /// The description from the package.json
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// The latest version of the package
        /// </summary>
        public string LatestVersion { get; private set; }

        /// <summary>
        /// The package name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// All versions for the package
        /// </summary>
        public IList<SemanticVersion> Versions { get; private set; }

        internal NpmPackageInfo(string name, string description, string latestVersion)
        {
            Name = name;
            Description = description;
            LatestVersion = latestVersion;
        }

        internal NpmPackageInfo(string name, string description, string latestVersion, IList<SemanticVersion> versions)
        {
            Name = name;
            Description = description;
            LatestVersion = latestVersion;
            Versions = versions ?? new List<SemanticVersion>();
        }

        internal static NpmPackageInfo Parse(JObject packageInfo)
        {
            string name = packageInfo.GetJObjectMemberStringValue("name");
            string description = packageInfo.GetJObjectMemberStringValue("description");
            string version = packageInfo.GetJObjectMemberStringValue("version");

            return new NpmPackageInfo(name, description, version);
        }
    }
}
