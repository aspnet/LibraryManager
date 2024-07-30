// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Web.LibraryManager.Tools.Test
{
    public class CommandTestBase
    {
        protected string WorkingDir { get; set; }
        protected string CacheDir { get; set; }
        internal IHostEnvironment HostEnvironment { get; set; }

        public virtual void Setup()
        {
            WorkingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(WorkingDir);
            CacheDir = Path.Combine(WorkingDir, "cache");
            Directory.CreateDirectory(CacheDir);

            HostEnvironment = TestEnvironmentHelper.GetTestHostEnvironment(WorkingDir, CacheDir);
        }

        public virtual void Cleanup()
        {
            try
            {
                Directory.Delete(WorkingDir, true);
            }
            catch
            {
                // Don't fail the tests if cleanup failed.
            }
        }
    }
}
