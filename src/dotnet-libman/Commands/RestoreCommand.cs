// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
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
            var result = await manifest.RestoreAsync(CancellationToken.None);

            var failures = result.Where(r => !r.Success);
            if (failures.Any())
            {
                var errors = failures.SelectMany(r => r.Errors);

                foreach (var e in errors)
                {
                    Logger.Log(string.Format(Resources.FailedToRestoreLibraryMessage, e.Code, e.Message), LogLevel.Error);
                }
            }

            return 0;
        }
    }
}
