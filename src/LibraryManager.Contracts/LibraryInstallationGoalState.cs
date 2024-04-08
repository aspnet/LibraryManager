// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        public LibraryInstallationGoalState(ILibraryInstallationState installationState)
        {
            InstallationState = installationState;
        }

        /// <summary>
        /// The ILibraryInstallationState that this goal state was computed from.
        /// </summary>
        public ILibraryInstallationState InstallationState { get; }

        /// <summary>
        /// Mapping from destination file to source file
        /// </summary>
        public IDictionary<string, string> InstalledFiles { get; } = new Dictionary<string, string>();

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
