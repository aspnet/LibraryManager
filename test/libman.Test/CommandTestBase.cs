using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Web.LibraryManager.Tools.Test
{
    public class CommandTestBase
    {
        protected string WorkingDir { get; set; }
        protected string CacheDir { get; set; }
        internal IHostEnvironment HostEnvironment { get; set; }

        public virtual void Setup()
        {
            WorkingDir = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString());
            CacheDir = Path.Combine(WorkingDir, "cache");
            Directory.CreateDirectory(CacheDir);

            HostEnvironment = TestEnvironmentHelper.GetTestHostEnvironment(WorkingDir, CacheDir);
        }

    }
}
