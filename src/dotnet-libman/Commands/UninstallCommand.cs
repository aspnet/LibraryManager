using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    internal class UninstallCommand : BaseCommand
    {
        public UninstallCommand(bool throwOnUnexpectedArg = true)
           : base(throwOnUnexpectedArg, "uninstall", Resources.UnInstallCommandDesc)
        {
        }

        public CommandArgument LibraryId { get; private set; }
        public CommandOption Provider { get; private set; }

        public override BaseCommand Configure(CommandLineApplication parent)
        {
            base.Configure(parent);

            this.LibraryId = this.Argument("libraryId", Resources.UninstallCommandLibraryIdArgumentDesc, false);
            this.Provider = this.Option("--provider", Resources.UninstallCommandProviderOptionDesc, CommandOptionType.SingleValue);

            return this;
        }

        protected override int ExecuteInternal()
        {
            this.PrintOptionsAndArguments();
            return 0;
        }

        public override string Remarks => Resources.UnInstallCommandRemarks;
        public override string Examples => Resources.UnInstallCommandExamples;
    }
}
