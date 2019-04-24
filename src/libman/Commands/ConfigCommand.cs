// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Contracts.Configuration;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    internal class ConfigCommand : BaseCommand
    {
        public CommandArgument FetchKey { get; private set; }
        public CommandOption SetPairs { get; private set; }
        public CommandOption SetEncryptedPairs { get; private set; }

        private readonly ISettings _settings;

        public ConfigCommand(IHostEnvironment hostEnvironment, bool throwOnUnexpectedArg = true)
            : base(throwOnUnexpectedArg, "config", Resources.Text.ConfigCommand_Description, hostEnvironment)
        {
            _settings = hostEnvironment.HostInteraction.Settings;
        }

        public override BaseCommand Configure(CommandLineApplication parent = null)
        {
            base.Configure(parent);

            FetchKey = Argument("key", Resources.Text.ConfigCommand_ArgumentDescription, multipleValues: false);
            SetPairs = Option("--set", Resources.Text.ConfigCommand_OptionDescription_Set, CommandOptionType.MultipleValue);
            SetEncryptedPairs = Option("--setEncrypted", Resources.Text.ConfigCommand_OptionDescription_SetEncrypted, CommandOptionType.MultipleValue);

            return this;
        }

        protected override Task<int> ExecuteInternalAsync()
        {
            ValidateParameters();

            if (string.IsNullOrWhiteSpace(FetchKey.Value))
            {
                SetSettings(SetPairs.Values);
                SetEncryptedSettings(SetEncryptedPairs.Values);
            }
            else
            {
                if(!_settings.TryGetValue(FetchKey.Value, out string fetchedValue))
                {
                    throw new InvalidOperationException(string.Format(Resources.Text.ConfigCommand_Error_KeyNotFound, FetchKey.Value));
                }

                HostEnvironment.Logger.Log(fetchedValue, LogLevel.Task);
            }

            return Task.FromResult(0);
        }

        private void SetSettings(List<string> values)
        {
            List<KeyValuePair<string, string>> pairs = SplitValues(values);

            foreach (KeyValuePair<string, string> pair in pairs)
            {
                if (string.IsNullOrEmpty(pair.Value))
                {
                    _settings.RemoveValue(pair.Key);
                }
                else
                {
                    _settings.SetValue(pair.Key, pair.Value);
                }
            }
        }

        private void SetEncryptedSettings(List<string> values)
        {
            List<KeyValuePair<string, string>> pairs = SplitValues(values);

            foreach (KeyValuePair<string, string> pair in pairs)
            {
                if (string.IsNullOrEmpty(pair.Value))
                {
                    _settings.RemoveValue(pair.Key);
                }
                else
                {
                    _settings.SetEncryptedValue(pair.Key, pair.Value);
                }
            }
        }

        private static List<KeyValuePair<string, string>> SplitValues(List<string> values)
        {
            var result = new List<KeyValuePair<string, string>>();

            foreach (string input in values)
            {
                int splice = input.IndexOf('=', StringComparison.Ordinal);
                // must have a name and an equals sign, otherwise just skip it
                if (splice < 1)
                {
                    continue;
                }

                string name = input.Substring(0, splice);
                string value = input.Substring(splice + 1);

                result.Add(new KeyValuePair<string, string>(name, value));
            }

            return result;
        }

        private void ValidateParameters()
        {
            var errors = new List<string>();

            if (!string.IsNullOrWhiteSpace(FetchKey.Value)
                && (SetPairs.Values.Any() || SetEncryptedPairs.Values.Any()))
            {
                errors.Add(Resources.Text.ConfigCommand_Error_ConflictingParameters);
            }

            if (errors.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            }
        }

        public override string Remarks => Resources.Text.ConfigCommand_Remarks;
        public override string Examples => Resources.Text.ConfigCommand_Examples;
    }
}
