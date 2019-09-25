using System;

namespace Microsoft.Web.LibraryManager
{
    /// <summary>
    /// Represents a semantic version
    /// </summary>
    public class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
    {
        private readonly int _hashCode;

        /// <summary>
        /// The major version value
        /// </summary>
        public int Major { get; private set; }

        /// <summary>
        /// The minor version value
        /// </summary>
        public int Minor { get; private set; }

        /// <summary>
        /// The patch version value
        /// </summary>
        public int Patch { get; private set; }

        /// <summary>
        /// The build metadata for the version
        /// </summary>
        public string BuildMetadata { get; private set; }

        /// <summary>
        /// The prerelease version
        /// </summary>
        public string PrereleaseVersion { get; private set; }

        /// <summary>
        /// The complete and original value of the semantic version
        /// </summary>
        public string OriginalText { get; private set; }

        private SemanticVersion(string originalText)
        {
            _hashCode = originalText?.GetHashCode() ?? 0;
            OriginalText = originalText;
        }

        internal static SemanticVersion Parse(string value)
        {
            SemanticVersion ver = new SemanticVersion(value);

            if (value == null)
            {
                return ver;
            }

            int prereleaseStart = value.IndexOf('-');
            int buildMetadataStart = value.IndexOf('+');

            //If the index of the build metadata marker (+) is greater than the index of the prerelease marker (-)
            //  then it is necessarily found in the string because if both were not found they'd be equal
            if (buildMetadataStart > prereleaseStart)
            {
                //If the build metadata marker is not the last character in the string, take off everything after it
                //  and use it for the build metadata field
                if (buildMetadataStart < value.Length - 1)
                {
                    ver.BuildMetadata = value.Substring(buildMetadataStart + 1);
                }

                value = value.Substring(0, buildMetadataStart);

                //If the prerelease section is found, extract it
                if (prereleaseStart > -1)
                {
                    //If the prerelease section marker is not the last character in the string, take off everything after it
                    //  and use it for the prerelease field
                    if (prereleaseStart < value.Length - 1)
                    {
                        ver.PrereleaseVersion = value.Substring(prereleaseStart + 1);
                    }

                    value = value.Substring(0, prereleaseStart);
                }
            }
            //If the build metadata wasn't the last metadata section found, check to see if a prerelease section exists.
            //  If it doesn't, then neither section exists
            else if (prereleaseStart > -1)
            {
                //If the prerelease version marker is not the last character in the string, take off everything after it
                //  and use it for the prerelease version field
                if (prereleaseStart < value.Length - 1)
                {
                    ver.PrereleaseVersion = value.Substring(prereleaseStart + 1);
                }

                value = value.Substring(0, prereleaseStart);

                //If the build metadata section is found, extract it
                if (buildMetadataStart > -1)
                {
                    //If the build metadata marker is not the last character in the string, take off everything after it
                    //  and use it for the build metadata field
                    if (buildMetadataStart < value.Length - 1)
                    {
                        ver.BuildMetadata = value.Substring(buildMetadataStart + 1);
                    }

                    value = value.Substring(0, buildMetadataStart);
                }
            }

            string[] versionParts = value.Split('.');

            if (versionParts.Length > 0)
            {
                int major;
                int.TryParse(versionParts[0], out major);
                ver.Major = major;
            }

            if (versionParts.Length > 1)
            {
                int minor;
                int.TryParse(versionParts[1], out minor);
                ver.Minor = minor;
            }

            if (versionParts.Length > 2)
            {
                int patch;
                int.TryParse(versionParts[2], out patch);
                ver.Patch = patch;
            }

            return ver;
        }

        /// <summary>
        /// Compares this object to the other and returns a result indicating sort order.
        /// </summary>
        /// <remarks>
        /// This comparison does take build metadata into account for the comparison.
        /// </remarks>
        /// <returns>-1 if this version is lower than other; 0 if they are equal; 1 otherwise.</returns>
        public int CompareTo(SemanticVersion other)
        {
            if (other == null)
            {
                return 1;
            }

            int result = Major.CompareTo(other.Major);

            if (result != 0)
            {
                return result;
            }

            result = Minor.CompareTo(other.Minor);

            if (result != 0)
            {
                return result;
            }

            result = Patch.CompareTo(other.Patch);

            if (result != 0)
            {
                return result;
            }

            //A version not marked with prerelease is later than one with a prerelease designation
            if (PrereleaseVersion == null && other.PrereleaseVersion != null)
            {
                return 1;
            }

            //A version not marked with prerelease is later than one with a prerelease designation
            if (PrereleaseVersion != null && other.PrereleaseVersion == null)
            {
                return -1;
            }

            result = StringComparer.OrdinalIgnoreCase.Compare(PrereleaseVersion, other.PrereleaseVersion);

            if (result != 0)
            {
                return result;
            }

            return StringComparer.OrdinalIgnoreCase.Compare(OriginalText, other.OriginalText);
        }

        /// <summary>
        /// Returns whether the semantic verisons are equal.  This includes comparing the build metadata, and does not provide semantic equivalence.
        /// </summary>
        public bool Equals(SemanticVersion other)
        {
            return other != null && string.Equals(OriginalText, other.OriginalText, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns whether the other object is equal to this SemanticVersion.
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as SemanticVersion);
        }

        /// <summary>
        /// Returns a hash code to uniquely identify this version.
        /// </summary>
        /// <remarks>
        /// This is aware of build metadata, and should not be relied on as a semantic equality comparison.
        /// </remarks>
        public override int GetHashCode()
        {
            return _hashCode;
        }

        /// <summary>
        /// Get a string representation of this SemanticVersion
        /// </summary>
        public override string ToString()
        {
            return OriginalText;
        }

        /// <summary>
        /// Equality operator
        /// </summary>
        public static bool operator ==(SemanticVersion left, SemanticVersion right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator
        /// </summary>
        public static bool operator !=(SemanticVersion left, SemanticVersion right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Less-than operator
        /// </summary>
        public static bool operator <(SemanticVersion left, SemanticVersion right)
        {
            return left is null ? !(right is null) : left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Less-than-or-equal operator
        /// </summary>
        public static bool operator <=(SemanticVersion left, SemanticVersion right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Greater-than operator
        /// </summary>
        public static bool operator >(SemanticVersion left, SemanticVersion right)
        {
            return !(left is null) && left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Greater-than-or-equal operator
        /// </summary>
        public static bool operator >=(SemanticVersion left, SemanticVersion right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
    }
}
