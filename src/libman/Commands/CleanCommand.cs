// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    /// <summary>
    /// Defines the clean command to allow user to clean the files installed via libman.
    /// </summary>
    internal class CleanCommand : BaseCommand
    {
        public CleanCommand(IHostEnvironment hostEnvironment, bool throwOnUnexpectedArg = true)
            : base(throwOnUnexpectedArg, "clean", Resources.CleanCommandDesc, hostEnvironment)
        {
        }

        public override string Remarks => Resources.CleanCommandRemarks;

        protected override async Task<int> ExecuteInternalAsync()
        {
            Manifest manifest = await GetManifestAsync();

            Task<bool> deleteFileAction(IEnumerable<string> s) => HostInteractions.DeleteFilesAsync(s, CancellationToken.None);

            IEnumerable<ILibraryInstallationResult> result = await manifest.CleanAsync(deleteFileAction, CancellationToken.None);

            IEnumerable<ILibraryInstallationResult> failures = result.Where(r => !r.Success);

            if (failures.Any())
            {
                Logger.Log(Resources.CleanFailed, LogLevel.Error);
            }

            return 0;
        }

    }
}
