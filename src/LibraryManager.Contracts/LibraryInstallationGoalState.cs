// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// Represents a goal state of deployed files mapped to their sources from the local cache
    /// </summary>
    public class LibraryInstallationGoalState
    {
        /// <summary>
        /// Initialize a new goal state from the desired installation state.
        /// </summary>
        public LibraryInstallationGoalState(ILibraryInstallationState installationState, Dictionary<string, string> installedFiles)
        {
            InstallationState = installationState;
            InstalledFiles = installedFiles;
        }

        /// <summary>
        /// The ILibraryInstallationState that this goal state was computed from.
        /// </summary>
        public ILibraryInstallationState InstallationState { get; }

        /// <summary>
        /// Mapping from destination file to source file
        /// </summary>
        public IDictionary<string, string> InstalledFiles { get; }

        /// <summary>
        /// Returns whether the goal is in an achieved state - that is, all files are up to date.
        /// </summary>
        /// <remarks>
        /// This is intended to serve as a fast check compared to restoring the files.  
        /// If there isn't a faster way to verify that a file is up to date, this method should
        /// return false to indicate that a restore can't be skipped.
        /// </remarks>
        public bool IsAchieved()
        {
            foreach (KeyValuePair<string, string> kvp in InstalledFiles)
            {
                // If the source file is a remote Uri, we have no way to determine if it matches the installed file.
                // So we will always reinstall the library in this case.
                if (FileHelpers.IsHttpUri(kvp.Value))
                {
                    return false;
                }

                var destinationFile = new FileInfo(kvp.Key);
                var cacheFile = new FileInfo(kvp.Value);

                if (!destinationFile.Exists || !cacheFile.Exists || !FileHelpers.AreFilesUpToDate(destinationFile, cacheFile))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
