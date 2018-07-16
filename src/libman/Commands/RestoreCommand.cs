// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    /// <summary>
    /// Defines the libman restore command.
    /// </summary>
    internal class RestoreCommand : BaseCommand
    {
        public RestoreCommand(IHostEnvironment hostEnvironment, bool throwOnUnexpectedArg = true)
            : base(throwOnUnexpectedArg, "restore", Resources.Text.RestoreCommandDesc, hostEnvironment)
        {
        }

        public override string Remarks => Resources.Text.RestoreCommandRemarks;

        protected override async Task<int> ExecuteInternalAsync()
        {
            var sw = new Stopwatch();
            sw.Start();

            Manifest manifest = await GetManifestAsync();
            IEnumerable<ILibraryOperationResult> validationResults = await manifest.GetValidationResultsAsync(CancellationToken.None);

            if (!validationResults.All(r => r.Success))
            {
                sw.Stop();
                LogErrors(validationResults.SelectMany(r => r.Errors));

                return 0;
            }

            IEnumerable<ILibraryOperationResult> results = await ManifestRestorer.RestoreManifestAsync(manifest, Logger, CancellationToken.None);
            sw.Stop();
            LogResultsSummary(results, OperationType.Restore, sw.Elapsed);

            return 0;
        }
    }
}
