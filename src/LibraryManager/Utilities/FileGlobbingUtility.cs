using System;
using System.Collections.Generic;
using System.Linq;
using Minimatch;

namespace Microsoft.Web.LibraryManager.Utilities
{
    internal class FileGlobbingUtility
    {
        private static readonly char[] GlobIndicatorCharacters = "*?[".ToCharArray();

        public static IEnumerable<string> ExpandFileGlobs(IEnumerable<string> potentialGlobs, IEnumerable<string> libraryFiles)
        {
            var finalSetOfFiles = new HashSet<string>();
            var negatedOptions = new Minimatch.Options { FlipNegate = true };

            foreach (string potentialGlob in potentialGlobs)
            {
                // only process globs where we find them, otherwise it can get expensive
                if (potentialGlob.StartsWith("!", StringComparison.Ordinal))
                {
                    // Remove matches from the files list
                    var filesToRemove = finalSetOfFiles.Where(f => Minimatcher.Check(f, potentialGlob, negatedOptions)).ToList();
                    foreach (string file in filesToRemove)
                    {
                        finalSetOfFiles.Remove(file);
                    }
                }
                else if (potentialGlob.IndexOfAny(GlobIndicatorCharacters) >= 0)
                {
                    IEnumerable<string> filterResult = libraryFiles.Where(f => Minimatcher.Check(f, potentialGlob));
                    if (filterResult.Any())
                    {
                        finalSetOfFiles.UnionWith(filterResult);
                    }
                }
                else
                {
                    // not a glob pattern, so just include the file literally
                    finalSetOfFiles.Add(potentialGlob);
                }
            }

            return finalSetOfFiles;
        }
    }
}
