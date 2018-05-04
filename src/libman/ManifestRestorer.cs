// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Tools
{
    internal static class ManifestRestorer
    {
        public static async Task RestoreManifestAsync(Manifest manifest, ILogger logger, CancellationToken cancelToken)
        {
            IEnumerable<ILibraryInstallationResult> result = await manifest.RestoreAsync(cancelToken);

            IEnumerable<ILibraryInstallationResult> failures = result.Where(r => !r.Success);
            if (failures.Any())
            {
                IEnumerable<IError> errors = failures.SelectMany(r => r.Errors);

                foreach (IError e in errors)
                {
                    logger.Log(string.Format(Resources.FailedToRestoreLibraryMessage, e.Code, e.Message), LogLevel.Error);
                }
            }
        }
    }
}
