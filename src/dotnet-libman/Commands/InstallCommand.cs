using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    internal class InstallCommand : BaseCommand
    {
        public InstallCommand(bool throwOnUnexpectedArg = true)
            : base(throwOnUnexpectedArg, "install", Resources.InstallCommandDesc)
        {
        }

        public CommandArgument LibraryId { get; private set; }
        public CommandOption Provider { get; private set; }
        public CommandOption Destination { get; set; }
        public CommandOption Files { get; set; }

        public override BaseCommand Configure(CommandLineApplication parent)
        {
            base.Configure(parent);

            this.LibraryId = this.Argument("libraryId", Resources.InstallCommandLibraryIdArgumentDesc, false);
            this.Provider = this.Option("--provider", Resources.ProviderOptionDesc, CommandOptionType.SingleValue);
            this.Destination = this.Option("--destination", Resources.DestinationOptionDesc, CommandOptionType.SingleValue);
            this.Files = this.Option("--files", Resources.FilesOptionDesc, CommandOptionType.MultipleValue);

            return this;
        }

        protected override int ExecuteInternal()
        {
            this.PrintOptionsAndArguments();
            return 0;
        }

        public override string Examples => Resources.InstallCommandExamples;
        public override string Remarks => Resources.InstallCommandRemarks;

    }
}
