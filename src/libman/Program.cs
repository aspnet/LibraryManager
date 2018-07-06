// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Tools.Commands;

namespace Microsoft.Web.LibraryManager.Tools
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            int debugIndex = args.ToList().FindIndex(a => a.Equals("--debug", StringComparison.OrdinalIgnoreCase));
            if (debugIndex > 0)
            {
                IEnumerable<string> newArgs = args.Take(debugIndex);
                args = newArgs.Concat(args.Skip(debugIndex + 1)).ToArray();
                Console.WriteLine($"Attach Debugger to process: {System.Diagnostics.Process.GetCurrentProcess().Id}");
                while (!System.Diagnostics.Debugger.IsAttached);
            }
#endif

            EnvironmentSettings defaultSettings = EnvironmentSettings.Default;
            var environment = HostEnvironment.Initialize(defaultSettings);
            var app = new LibmanApp(environment, true);

            app.Configure();
            try
            {
                app.Execute(args);
            }
            catch (CommandParsingException cpe)
            {
                defaultSettings.Logger.Log(string.Format(Resources.Text.InvalidArgumentsMessage, cpe.Command.Name), LogLevel.Error);
                cpe.Command.ShowHelp();
            }
            catch (AggregateException ae)
            {
                foreach (Exception ie in ae.InnerExceptions)
                {
                    defaultSettings.Logger.Log(ie.Message, LogLevel.Error);
                }
            }
            catch (Exception ex)
            {
                defaultSettings.Logger.Log(ex.Message, LogLevel.Error);
            }
        }
    }
}
