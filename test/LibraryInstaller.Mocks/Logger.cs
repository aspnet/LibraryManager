using LibraryInstaller.Contracts;
using System;
using System.Collections.Generic;

namespace LibraryInstaller.Mocks
{
    /// <summary>
    /// A mock of the <see cref="ILogger"/> interface.
    /// </summary>
    /// <seealso cref="LibraryInstaller.Contracts.ILogger" />
    public class Logger : ILogger
    {
        /// <summary>
        /// A list of all the log messages recorded by the <see cref="Log"/> method.
        /// </summary>
        public List<Tuple<string, Level>> Messages = new List<Tuple<string, Level>>();

        /// <summary>
        /// Logs the specified message to the host.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The level of the message.</param>
        public void Log(string message, Level level)
        {
            Messages.Add(Tuple.Create(message, level));
        }
    }
}
