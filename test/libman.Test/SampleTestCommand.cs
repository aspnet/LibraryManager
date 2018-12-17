// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Tools.Commands;

namespace Microsoft.Web.LibraryManager.Tools.Test
{
    internal class SampleTestCommand : BaseCommand
    {
        public bool CreateNewManifest { get; set; }
        public string DefaultProvider { get; set; }
        public string DefaultDestination { get; set; }

        public SampleTestCommand(IHostEnvironment environment) 
            : base(false, "Sample", "", environment)
        {
        }

        public Manifest Manifest { get; private set; }

        protected override async Task<int> ExecuteInternalAsync()
        {
            if (CreateNewManifest)
            {
                Manifest = await CreateManifestAsync(DefaultProvider, DefaultDestination, Settings, "", CancellationToken.None);
            }
            else
            {
                Manifest = await GetManifestAsync();
            }

            return 0;
        }
    }
}
