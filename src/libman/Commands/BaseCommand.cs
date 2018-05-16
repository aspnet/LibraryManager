// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Tools.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    /// <summary>
    /// Provides an abstract base implementation for all libman commands.
    /// </summary>
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

        /// <summary>
        /// Lets user specify --verbosity to see more output.
        /// </summary>
        public CommandOption Verbosity { get; private set; }

        /// <summary>
        /// Lets user to run libman commands by specifying a different root directory.
        /// </summary>
        /// <remarks>Currently unused.</remarks>
        public CommandOption RootDir { get; private set; }

        /// <summary>
        /// Remarks to be shown with the help text.
        /// </summary>
        public virtual string Remarks { get; } = null;

        /// <summary>
        /// Examples to be shown with the help text.
        /// </summary>
        public virtual string Examples { get; } = null;

        protected ILogger Logger => HostEnvironment.Logger;
        protected IDependencies ManifestDependencies { get; private set; }
        protected IHostInteractionInternal HostInteractions => HostEnvironment?.HostInteraction;
        protected IHostEnvironment HostEnvironment { get; }
        protected EnvironmentSettings Settings => HostEnvironment.EnvironmentSettings;

        /// <summary>
        /// Sets up the arugments and options and also defines the operation
        /// executed by the command during execution.
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public virtual BaseCommand Configure(CommandLineApplication parent = null)
        {
            HelpOption("--help|-h");
            Verbosity = Option("--verbosity", Resources.VerbosityOptionDesc, CommandOptionType.SingleValue);
            RootDir = Option("--root", Resources.ProjectPathOptionDesc, CommandOptionType.SingleValue);

            // Reserving this for now.
            RootDir.ShowInHelpText = false;

            OnExecute(async () =>
            {
                if (RootDir.HasValue())
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
            string projectPath = RootDir.Value();
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

        protected virtual Task<int> ExecuteInternalAsync()
        {
            ShowHelp();

            return Task.FromResult(0);
        }

        /// <summary>
        /// Provides the help text for the command.
        /// </summary>
        /// <param name="commandName"></param>
        /// <returns></returns>
        public override string GetHelpText(string commandName = null)
        {
            string baseHelp = base.GetHelpText(commandName).Replace("dotnet libman", "libman");
            var help = new StringBuilder(baseHelp);

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

        protected async Task<Manifest> GetManifestAsync(bool createIfNotExists = false)
        {
            if (!File.Exists(Settings.ManifestFileName) && !createIfNotExists)
            {
                throw new InvalidOperationException(string.Format(Resources.LibmanJsonNotFound, Settings.ManifestFileName));
            }

            IDependencies dependencies = ManifestDependencies;
            Manifest manifest = await Manifest.FromFileAsync(Settings.ManifestFileName, dependencies, CancellationToken.None);

            if (manifest == null)
            {
                throw new InvalidOperationException(Resources.FixManifestFile);
            }

            if (!File.Exists(Settings.ManifestFileName))
            {
                manifest.AddVersion(Manifest.SupportedVersions.Last().ToString());
            }

            return manifest;
        }
    }
}
