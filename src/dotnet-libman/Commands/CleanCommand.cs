// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
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
            IEnumerable<ILibraryInstallationResult> result = manifest.Clean((s) => HostEnvironment.HostInteraction.DeleteFiles(s));
            IEnumerable<ILibraryInstallationResult> failures = result.Where(r => !r.Success);

            if (failures.Any())
            {
                Logger.Log(Resources.CleanFailed, LogLevel.Error);
            }

            return 0;
        }

    }
}
