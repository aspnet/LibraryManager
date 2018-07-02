using Microsoft.Web.LibraryManager.Providers.Unpkg;

namespace Microsoft.Web.LibraryManager.Vsix.Json.Completion
{
    internal static class CompletionUtility
    {
        internal static int CompareSemanticVersion(SemanticVersion selfSemVersion, SemanticVersion otherSemVersion)
        {
            if (selfSemVersion == null)
            {
                if (otherSemVersion != null)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                if (otherSemVersion != null)
                {
                    return -selfSemVersion.CompareTo(otherSemVersion);
                }
                else
                {
                    return -1;
                }
            }
        }
    }
}
