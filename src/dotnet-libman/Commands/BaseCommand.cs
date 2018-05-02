// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Tools.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    internal abstract class BaseCommand : CommandLineApplication
    {
        public BaseCommand(bool throwOnUnexpectedArg, string commandName, string description, IHostEnvironment environment)
            : base(throwOnUnexpectedArg)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                throw new ArgumentException(nameof(commandName));
            }

            HostEnvironment = environment ?? throw new ArgumentNullException(nameof(environment));

            Name = commandName;
            Description = description;
        }

        public CommandOption Verbosity { get; private set; }
        public CommandOption Project { get; private set; }
        protected ILogger Logger => HostEnvironment.Logger;
        protected IDependencies ManifestDependencies { get; private set; }
        protected IHostInteractionInternal HostInteractions => HostEnvironment?.HostInteraction;
        public virtual string Remarks { get; } = null;
        public virtual string Examples { get; } = null;
        protected IHostEnvironment HostEnvironment { get; }
        protected EnvironmentSettings Settings => HostEnvironment.EnvironmentSettings;

        public virtual BaseCommand Configure(CommandLineApplication parent = null)
        {
            HelpOption("--help|-h");
            Verbosity = Option("--verbosity", Resources.VerbosityOptionDesc, CommandOptionType.SingleValue);
            Project = Option("--project|-p", Resources.ProjectPathOptionDesc, CommandOptionType.SingleValue);

            OnExecute(async () =>
            {
                if (Project.HasValue())
                {
                    HostEnvironment.UpdateWorkingDirectory(GetProjectDirectory());
                }

                InitializeDependencies();

                return await ExecuteInternalAsync();
            });
            Parent = parent;

            return this;
        }

        private void InitializeDependencies()
        {
            ManifestDependencies = new Dependencies(HostEnvironment);
        }

        private string GetProjectDirectory()
        {
            string projectPath = Project.Value();
            if (!Path.IsPathRooted(projectPath))
            {
                projectPath = Path.Combine(Directory.GetCurrentDirectory(), projectPath);
            }

            if (File.Exists(projectPath))
            {
                projectPath = Path.GetDirectoryName(projectPath);
            }

            if (!Directory.Exists(projectPath))
            {
                throw new DirectoryNotFoundException(string.Format(Resources.DirectoryNotFoundMessage, projectPath));
            }

            return projectPath;
        }

        protected virtual int ExecuteInternal()
        {
            ShowHelp();
            return 0;
        }

        protected virtual Task<int> ExecuteInternalAsync()
        {
            ShowHelp();

            return Task.FromResult(0);
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



        protected void PrintOptionsAndArguments()
        {
            StringBuilder outputStr = new StringBuilder($"Options:{Environment.NewLine}");
            if (Options != null)
            {
                foreach (var o in Options)
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

            if (Arguments != null)
            {
                foreach (var arg in Arguments)
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

        protected async Task<Manifest> GetManifestAsync()
        {
            IDependencies dependencies = ManifestDependencies;
            Manifest manifest = await Manifest.FromFileAsync(Settings.ManifestFileName, dependencies, CancellationToken.None);
            return manifest;
        }
    }
}
