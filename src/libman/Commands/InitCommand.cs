// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    /// <summary>
    /// Defines the libman init command, to allow users to create a libman.json file in the current directory.
    /// </summary>
    internal class InitCommand : BaseCommand
    {
        public InitCommand(IHostEnvironment hostEnvironment, bool throwOnUnexpectedArg = true)
            : base(throwOnUnexpectedArg, "init", Resources.InitCommandDesc, hostEnvironment)
        {
        }

        /// <summary>
        /// Option to allow users to specify the default provider for the libman.json
        /// </summary>
        public CommandOption DefaultProvider { get; private set; }

        /// <summary>
        /// Option to allow users to specify the default destination for the libman.json
        /// </summary>
        public CommandOption DefaultDestination { get; private set; }


        public override BaseCommand Configure(CommandLineApplication parent)
        {
            base.Configure(parent);

            DefaultProvider = Option("--default-provider|-p", Resources.DefaultProviderOptionDesc, CommandOptionType.SingleValue);
            DefaultDestination = Option("--default-destination|-d", Resources.DefaultDestinationOptionDesc, CommandOptionType.SingleValue);

            return this;
        }

        protected async override Task<int> ExecuteInternalAsync()
        {
            FailIfLibmanJsonExists();
            string defaultProvider = string.Empty;
            string defaultDestination = string.Empty;

            defaultProvider = DefaultProvider.HasValue() 
                ? DefaultProvider.Value() 
                : HostEnvironment.InputReader.GetUserInput($"{nameof(DefaultProvider)}:");

            defaultDestination = DefaultDestination.HasValue()
                ? DefaultDestination.Value()
                : HostEnvironment.InputReader.GetUserInput($"{nameof(DefaultDestination)}:");

            Manifest manifest = await GetManifestAsync(createIfNotExists: true);
            manifest.DefaultDestination = defaultDestination;
            manifest.DefaultProvider = defaultProvider;

            await manifest.SaveAsync(Settings.ManifestFileName, CancellationToken.None);

            return 0;
        }

        private void FailIfLibmanJsonExists()
        {
            if (File.Exists(Settings.ManifestFileName))
            {
                throw new Exception(Resources.InitFailedLibmanJsonFileExists);
            }
        }
    }
}
