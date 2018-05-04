using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    internal class UpdateCommand : BaseCommand
    {
        public UpdateCommand(IHostEnvironment environment, bool throwOnUnexpectedArg=true)
            : base(throwOnUnexpectedArg, "update", Resources.UpdateCommandDesc, environment)
        {
        }

        public override string Remarks => Resources.UpdateCommandRemarks;

        public override string Examples => Resources.UpdateCommandExamples;

        public CommandArgument LibraryId { get; private set; }
        public CommandOption Provider { get; private set; }
        public CommandOption PreRelease { get; private set; }

        public override BaseCommand Configure(CommandLineApplication parent = null)
        {
            base.Configure(parent);

            LibraryId = Argument("libraryId", Resources.UpdateCommandLibraryArgumentDesc, multipleValues: false);
            Provider = Option("--provider|-p", Resources.UpdateCommandProviderOptionDesc, CommandOptionType.SingleValue);
            PreRelease = Option("-pre", Resources.UpdateCommandPreReleaseOptionDesc, CommandOptionType.NoValue);

            return this;
        }

        protected override Task<int> ExecuteInternalAsync()
        {
            return base.ExecuteInternalAsync();
        }
    }
}
