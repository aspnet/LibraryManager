// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    internal class CacheListCommand : BaseCommand
    {
        public CacheListCommand(IHostEnvironment environment, bool throwOnUnexpectedArg = true)
            : base(throwOnUnexpectedArg, "list", Resources.CacheListCommandDesc, environment)
        {
        }

        public CommandOption Detailed { get; private set; }

        public override BaseCommand Configure(CommandLineApplication parent = null)
        {
            base.Configure(parent);

            Detailed = Option("--detailed|-d", Resources.CacheListDetailedOptionDesc, CommandOptionType.NoValue);

            return this;
        }

        protected override Task<int> ExecuteInternalAsync()
        {
            StringBuilder outputStr = new StringBuilder(Resources.CacheContentMessage);
            outputStr.Append(Environment.NewLine);
            outputStr.Append('-', Resources.CacheContentMessage.Length);
            outputStr.Append(Environment.NewLine);

            string cacheRoot = HostEnvironment.EnvironmentSettings.CacheDirectory;

            foreach (IProvider provider in ManifestDependencies.Providers)
            {
                outputStr.AppendLine(provider.Id+":");

                string providerCachePath = Path.Combine(cacheRoot, provider.Id);
                if (Directory.Exists(providerCachePath))
                {
                    IEnumerable<string> libraries = Directory.EnumerateDirectories(providerCachePath);
                    foreach(string library in libraries)
                    {
                        outputStr.Append(' ', 4);
                        outputStr.AppendLine(Path.GetFileName(library));
                        if (Detailed.HasValue())
                        {
                            IEnumerable<string> details = Directory.EnumerateFiles(library, "*", SearchOption.AllDirectories);
                            foreach(string detail in details)
                            {
                                outputStr.Append(' ', 8);
                                string detailStr = detail.Substring(library.Length);
                                if (detailStr.StartsWith(Path.DirectorySeparatorChar) || detailStr.StartsWith(Path.AltDirectorySeparatorChar))
                                {
                                    detailStr = detailStr.Substring(1);
                                }

                                outputStr.AppendLine(detailStr);
                            }
                        }
                    }
                }
                else
                {
                    outputStr.Append(' ', 4);
                    outputStr.AppendLine(Resources.CacheEmptyMessage);
                }
            }

            Logger.Log(outputStr.ToString(), LogLevel.Operation);

            return Task.FromResult(0);
        }
    }
}
