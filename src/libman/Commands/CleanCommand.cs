// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            : base(throwOnUnexpectedArg, "clean", Resources.Text.CleanCommandDesc, hostEnvironment)
        {
        }

        public override string Remarks => Resources.Text.CleanCommandRemarks;

        protected override async Task<int> ExecuteInternalAsync()
        {
            var sw = new Stopwatch();
            sw.Start();

            Manifest manifest = await GetManifestAsync();
            Task<bool> deleteFileAction(IEnumerable<string> s) => HostInteractions.DeleteFilesAsync(s, CancellationToken.None);
            IEnumerable<ILibraryOperationResult> validationResults = await manifest.GetValidationResultsAsync(CancellationToken.None);

            if (!validationResults.All(r => r.Success))
            {
                sw.Stop();
                LogErrors(validationResults.SelectMany(r => r.Errors));

                return 0;
            }

            IEnumerable<ILibraryOperationResult> results = await manifest.CleanAsync(deleteFileAction, CancellationToken.None);
            sw.Stop();
            LogResultsSummary(results, OperationType.Clean, sw.Elapsed);

            return 0;
        }
    }
}
