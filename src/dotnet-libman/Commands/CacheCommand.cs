using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    internal class CacheCommand : BaseCommand
    {
        public CacheCommand(bool throwOnUnexpectedArg = true) 
            : base(throwOnUnexpectedArg, "cache", Resources.CacheCommandDesc)
        {
        }


        public override BaseCommand Configure(CommandLineApplication parent)
        {
            return base.Configure(parent);
        }

        protected override int ExecuteInternal()
        {
            return base.ExecuteInternal();
        }
    }
}
