using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    internal class RestoreCommand : BaseCommand
    {
        public RestoreCommand(bool throwOnUnexpectedArg = true) 
            : base(throwOnUnexpectedArg, "restore", Resources.RestoreCommandDesc)
        {
        }

        public override string Remarks => Resources.RestoreCommandRemarks;
    }
}
