// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            Environment.ExitCode = (int)ExitCode.Failure;
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
                Environment.ExitCode = app.Execute(args);
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
