// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    /// <summary>
    /// Defines the libman restore command.
    /// </summary>
    internal class RestoreCommand : BaseCommand
    {
        public RestoreCommand(IHostEnvironment hostEnvironment, bool throwOnUnexpectedArg = true) 
            : base(throwOnUnexpectedArg, "restore", Resources.RestoreCommandDesc, hostEnvironment)
        {
        }

        public override string Remarks => Resources.RestoreCommandRemarks;

        protected override async Task<int> ExecuteInternalAsync()
        {
            Manifest manifest = await GetManifestAsync();
            await ManifestRestorer.RestoreManifestAsync(manifest, Logger, CancellationToken.None);

            return 0;
        }
    }
}
