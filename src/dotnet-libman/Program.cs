using System;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;
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
            LibmanApp app = new LibmanApp(true);
            app.Configure();
            try
            {
                app.Execute(args);
            }
            catch (CommandParsingException cpe)
            {
                Console.WriteLine(string.Format(Resources.InvalidArgumentsMessage, cpe.Command.Name));
                cpe.Command.ShowHelp();
            }
        }
    }
}
