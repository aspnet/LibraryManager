// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    internal class UninstallCommand : BaseCommand
    {
        public UninstallCommand(IHostEnvironment hostEnvironment, bool throwOnUnexpectedArg = true)
           : base(throwOnUnexpectedArg, "uninstall", Resources.UnInstallCommandDesc, hostEnvironment)
        {
        }

        public CommandArgument LibraryId { get; private set; }
        public CommandOption Provider { get; private set; }

        public override BaseCommand Configure(CommandLineApplication parent)
        {
            base.Configure(parent);

            LibraryId = Argument("libraryId", Resources.UninstallCommandLibraryIdArgumentDesc, false);
            Provider = Option("--provider", Resources.UninstallCommandProviderOptionDesc, CommandOptionType.SingleValue);

            return this;
        }

        protected override int ExecuteInternal()
        {
            PrintOptionsAndArguments();
            return 0;
        }

        public override string Remarks => Resources.UnInstallCommandRemarks;
        public override string Examples => Resources.UnInstallCommandExamples;
    }
}
