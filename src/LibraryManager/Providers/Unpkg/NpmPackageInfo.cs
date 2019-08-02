using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    /// <summary>
    /// Encapsulates information for an NPM package
    /// </summary>
    public sealed class NpmPackageInfo
    {
        /// <summary>
        /// The name, email, url of author listed in package.json
        /// </summary>
        public string Author { get; private set; }

        /// <summary>
        /// The description from the package.json
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// The latest version of the package
        /// </summary>
        public string LatestVersion { get; private set; }

        /// <summary>
        /// The homepage listed in the package.json
        /// </summary>
        public string Homepage { get; private set; }

        /// <summary>
        /// License as listed in package.json
        /// </summary>
        public string License { get; private set; }

        /// <summary>
        /// The package name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// All versions for the package
        /// </summary>
        public IList<SemanticVersion> Versions { get; private set; }

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
            JObject authorObject = packageInfo["author"] as JObject;
            if (authorObject != null)
            {
                author = authorObject.GetJObjectMemberStringValue("name");
            }

            return new NpmPackageInfo(name, description, version, author, homepage, license);
        }
    }
}
