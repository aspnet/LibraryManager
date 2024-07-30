﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    /// <summary>
    /// Defines the cache command for libman.
    /// Child commands: clean, list
    /// </summary>
    internal class CacheCommand : BaseCommand
    {
        public CacheCommand(IHostEnvironment hostEnvironment, bool throwOnUnexpectedArg = true) 
            : base(throwOnUnexpectedArg, "cache", Resources.Text.CacheCommandDesc, hostEnvironment)
        {
        }

        public override BaseCommand Configure(CommandLineApplication parent)
        {
            base.Configure(parent);

            Commands.Add(new CacheCleanCommand(HostEnvironment).Configure(this));
            Commands.Add(new CacheListCommand(HostEnvironment).Configure(this));

            return this;
        }
    }
}
