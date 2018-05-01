using System;
using System.Text;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    internal abstract class BaseCommand : CommandLineApplication
    {
        public BaseCommand(bool throwOnUnexpectedArg, string commandName, string description)
            : base(throwOnUnexpectedArg)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                throw new ArgumentException(nameof(commandName));
            }

            this.Name = commandName;
            this.Description = description;
        }

        public virtual BaseCommand Configure(CommandLineApplication parent = null)
        {
            this.HelpOption("--help|-h");
            
            this.OnExecute(() => ExecuteInternal());
            this.Parent = parent;


            return this;
        }

        protected virtual int ExecuteInternal()
        {
            ShowHelp();
            return 0;
        }

        public override string GetHelpText(string commandName = null)
        {
            StringBuilder help = new StringBuilder(base.GetHelpText(commandName));

            if (!string.IsNullOrWhiteSpace(Remarks))
            {
                help.Append($"{Environment.NewLine}{Resources.RemarksHeader}{Environment.NewLine}");
                help.Append($"{Remarks}{Environment.NewLine}");
            }

            if (!string.IsNullOrWhiteSpace(Examples))
            {
                help.Append($"{Environment.NewLine}{Resources.ExamplesHeader}{Environment.NewLine}");
                help.Append($"{Examples}{Environment.NewLine}");
            }

            return help.ToString();
        }

        public virtual string Remarks { get; } = null;

        public virtual string Examples { get; } = null;

        protected void PrintOptionsAndArguments()
        {
            StringBuilder outputStr = new StringBuilder($"Options:{Environment.NewLine}");
            if (this.Options != null)
            {
                foreach (var o in this.Options)
                {
                    outputStr.Append($"    {o.LongName}: ");
                    if (o.HasValue())
                    {
                        switch (o.OptionType)
                        {
                            case CommandOptionType.MultipleValue:
                                outputStr.Append(string.Join("; ", o.Values));
                                break;
                            case CommandOptionType.NoValue:
                                outputStr.Append("Flag specified");
                                break;
                            case CommandOptionType.SingleValue:
                                outputStr.Append(o.Value());
                                break;
                        }

                    }
                    else
                    {
                        outputStr.Append("Unspecified");
                    }

                    outputStr.Append(Environment.NewLine);
                }
            }

            outputStr.Append($"{Environment.NewLine}Argument:{Environment.NewLine}");

            if (this.Arguments != null)
            {
                foreach (var arg in this.Arguments)
                {
                    outputStr.Append($"    {arg.Name}: ");
                    if (arg.MultipleValues)
                    {
                        outputStr.Append(string.Join(";", arg.Values));
                    }
                    else
                    {
                        outputStr.Append(arg.Value);
                    }

                    outputStr.Append(Environment.NewLine);
                }
            }

            Console.WriteLine(outputStr.ToString());
        }
    }
}
