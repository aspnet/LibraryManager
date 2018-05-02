// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    internal class InstallCommand : BaseCommand
    {
        public InstallCommand(IHostEnvironment hostEnvironment, bool throwOnUnexpectedArg = true)
            : base(throwOnUnexpectedArg, "install", Resources.InstallCommandDesc, hostEnvironment)
        {
        }

        public CommandArgument LibraryId { get; private set; }
        public CommandOption Provider { get; private set; }
        public CommandOption Destination { get; set; }
        public CommandOption Files { get; set; }

        public override BaseCommand Configure(CommandLineApplication parent)
        {
            base.Configure(parent);

            LibraryId = Argument("libraryId", Resources.InstallCommandLibraryIdArgumentDesc, false);
            Provider = Option("--provider", Resources.ProviderOptionDesc, CommandOptionType.SingleValue);
            Destination = Option("--destination", Resources.DestinationOptionDesc, CommandOptionType.SingleValue);
            Files = Option("--files", Resources.FilesOptionDesc, CommandOptionType.MultipleValue);

            return this;
        }

        protected override async Task<int> ExecuteInternalAsync()
        {
            Manifest manifest = await GetManifestAsync();


            return 0;
        }

        public override string Examples => Resources.InstallCommandExamples;
        public override string Remarks => Resources.InstallCommandRemarks;

    }
}
