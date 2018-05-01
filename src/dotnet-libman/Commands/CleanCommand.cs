using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    internal class CleanCommand : BaseCommand
    {
        public CleanCommand(bool throwOnUnexpectedArg = true)
            : base(throwOnUnexpectedArg, "clean", Resources.CleanCommandDesc)
        {
        }

        public override string Remarks => Resources.CleanCommandRemarks;

    }
}
