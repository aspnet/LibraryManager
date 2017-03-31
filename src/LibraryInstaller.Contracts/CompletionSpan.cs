using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LibraryInstaller.Contracts
{
    /// <summary>
    /// A span for use by completion
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CompletionSpan
    {
        // IMPORTANT: Do not change the order of the fields below!!!

        /// <summary>
        /// The start position of the span
        /// </summary>
        public int Start;

        /// <summary>
        /// The length of the span
        /// </summary>
        public int Length;

        /// <summary>
        /// The list of completions for the span
        /// </summary>
        public IDictionary<string, string> Completions;
    }
}
