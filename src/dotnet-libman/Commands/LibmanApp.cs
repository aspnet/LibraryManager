using System;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    internal class LibmanApp : BaseCommand
    {
        public LibmanApp(bool throwOnUnexpectedArg = true) 
            : base(throwOnUnexpectedArg, "dotnet libman", Resources.LibmanCommandDesc)
        {

        }

        public CommandOption Verbosity { get; private set; }

        public override BaseCommand Configure(CommandLineApplication parent = null)
        {
            base.Configure(parent);

            this.VersionOption("--version", "1.0.0", "1.0.0.0");
            this.Verbosity = this.Option("--verbosity", Resources.VerbosityOptionDesc, CommandOptionType.SingleValue);

            this.Commands.Add(new InitCommand().Configure(this));
            this.Commands.Add(new InstallCommand().Configure(this));
            this.Commands.Add(new UninstallCommand().Configure(this));
            this.Commands.Add(new RestoreCommand().Configure(this));
            this.Commands.Add(new CleanCommand().Configure(this));
            this.Commands.Add(new CacheCommand().Configure(this));

            return this;
        }
    }
}