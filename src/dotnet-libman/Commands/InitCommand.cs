using System;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    internal class InitCommand : BaseCommand
    {
        public InitCommand(bool throwOnUnexpectedArg = true)
            : base(throwOnUnexpectedArg, "init", Resources.InitCommandDesc)
        {
        }

        public CommandOption DefaultProvider { get; private set; }
        public CommandOption DefaultDestination { get; private set; }


        public override BaseCommand Configure(CommandLineApplication parent)
        {
            base.Configure(parent);

            DefaultProvider = this.Option("--default-provider|-p", Resources.DefaultProviderOptionDesc, CommandOptionType.SingleValue);

            DefaultDestination = this.Option("--default-destination|-d", Resources.DefaultDestinationOptionDesc, CommandOptionType.SingleValue);

            return this;
        }

        protected override int ExecuteInternal()
        {
            this.PrintOptionsAndArguments();

            return 0;
        }
    }
}
