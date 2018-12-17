// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Contracts
{
    /// <summary>
    /// A singleton logger and input reader used by libman tool.
    /// </summary>
    internal class ConsoleLogger : ILogger, IInputReader
    {
        private object _syncObject = new object();

        private ConsoleLogger()
        {
            Console.OutputEncoding = Encoding.UTF8;
        }

        public static ConsoleLogger Instance { get; } = new ConsoleLogger();

        /// <summary>
        /// Gets user input by displaying the <paramref name="fieldName"/>
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public string GetUserInput(string fieldName)
        {
            ThrowIfInputIsRedirected();

            lock (_syncObject)
            {
                Console.Out.Write($"{fieldName}: ");
                return Console.ReadLine();
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public string GetUserInputWithDefault(string fieldName, string defaultValue)
        {
            ThrowIfInputIsRedirected();

            lock (_syncObject)
            {
                string message = $"{fieldName} [{defaultValue}]: ";
                Console.Out.Write(message);
                string value = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(value))
                {
                    value = defaultValue;
                }

                return value;
            }
        }

        /// <summary>
        /// Logs the <paramref name="message"/> to the console.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public void Log(string message, LogLevel level)
        {
            lock (_syncObject)
            {
                if (level == LogLevel.Error)
                {
                    if (Console.BackgroundColor != ConsoleColor.Red)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }

                    Console.Error.WriteLine(message);

                    Console.ResetColor();
                }
                else
                {
                    Console.Out.WriteLine(message);
                }
            }
        }

        private void ThrowIfInputIsRedirected()
        {
            if (Console.IsInputRedirected)
            {
                throw new InvalidOperationException(Resources.Text.NonInteractiveConsoleMessage);
            }
        }
    }
}
