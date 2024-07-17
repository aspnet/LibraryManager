// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Tools
{
    /// <summary>
    /// Helper class to restore a manifest and display errors if any.
    /// </summary>
    internal static class ManifestRestorer
    {
        /// <summary>
        /// Restore a manifest and display errors if any.
        /// </summary>
        /// <param name="manifest"></param>
        /// <param name="logger"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public static async Task<IList<ILibraryOperationResult>> RestoreManifestAsync(Manifest manifest, ILogger logger, CancellationToken cancelToken)
        {
            IList<ILibraryOperationResult> results = await manifest.RestoreAsync(cancelToken);

            IList<ILibraryOperationResult> failures = results.Where(r => r.Errors.Any()).ToList();
            if (failures.Any())
            {
                var librarySpecificErrors = new StringBuilder();
                var otherErrors = new StringBuilder();
                foreach (ILibraryOperationResult f in failures)
                {
                    if (f.InstallationState != null)
                    {
                        librarySpecificErrors.AppendLine(string.Format(Resources.Text.FailedToRestoreLibraryMessage, f.InstallationState.ToConsoleDisplayString()));
                        foreach (IError e in f.Errors)
                        {
                            librarySpecificErrors.AppendLine($"  - [{e.Code}]: {e.Message}");
                        }
                    }
                    else
                    {
                        foreach (IError e in f.Errors)
                        {
                            otherErrors.AppendLine($"[{e.Code}]: {e.Message}");
                        }
                    }
                }

                logger.Log(librarySpecificErrors.ToString(), LogLevel.Error);
                if (otherErrors.Length > 0)
                {
                    logger.Log(otherErrors.ToString(), LogLevel.Error);
                }
            }

            return results;
        }
    }
}
