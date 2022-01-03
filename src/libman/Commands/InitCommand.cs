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
            : base(throwOnUnexpectedArg, "init", Resources.Text.InitCommandDesc, hostEnvironment)
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

        /// <summary>
        /// Option to use all the default choices silently
        /// </summary>
        public CommandOption UseDefault { get; private set; }


        public override BaseCommand Configure(CommandLineApplication parent)
        {
            base.Configure(parent);

            DefaultProvider = Option("--default-provider|-p", Resources.Text.DefaultProviderOptionDesc, CommandOptionType.SingleValue);
            DefaultDestination = Option("--default-destination|-d", Resources.Text.DefaultDestinationOptionDesc, CommandOptionType.SingleValue);
            UseDefault = Option("--useDefault|-y", Resources.Text.UseDefault, CommandOptionType.NoValue);

            return this;
        }

        protected async override Task<int> ExecuteInternalAsync()
        {
            string defaultProvider = DefaultProvider.Value();
            if (string.IsNullOrEmpty(defaultProvider) && UseDefault.HasValue())
            {
                defaultProvider = Settings.DefaultProvider;
            }
            await CreateManifestAsync(defaultProvider, DefaultDestination.Value(), Settings, DefaultProvider.LongName, CancellationToken.None);

            return 0;
        }
    }
}
