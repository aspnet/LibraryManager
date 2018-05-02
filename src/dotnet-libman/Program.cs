// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
            if (args.Contains("--debug"))
            {
                args = args.SkipLast(1).ToArray();
                Console.WriteLine($"Attach Debugger to process: {System.Diagnostics.Process.GetCurrentProcess().Id}");
                while (!System.Diagnostics.Debugger.IsAttached);
            }
#endif

            EnvironmentSettings defaultSettings = EnvironmentSettings.Default;
            HostEnvironment environment = HostEnvironment.Initialize(defaultSettings);
            LibmanApp app = new LibmanApp(environment, true);

            app.Configure();
            try
            {
                app.Execute(args);
            }
            catch (CommandParsingException cpe)
            {
                defaultSettings.Logger.Log(string.Format(Resources.InvalidArgumentsMessage, cpe.Command.Name), LogLevel.Error);
                cpe.Command.ShowHelp();
            }
            catch (AggregateException ae)
            {
                //defaultSettings.Logger.Log(ae.Message, LogLevel.Error);
                foreach (var ie in ae.InnerExceptions)
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
