// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Tools.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Test
{
    [TestClass]
    public class HostInteractionTests
    {
        [TestMethod]
        public async Task Test_DeleteFilesAsync()
        {
            string workingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(workingDir);

            EnvironmentSettings settings = TestEnvironmentHelper.GetTestSettings(
                workingDir,
                Path.Combine(workingDir, "cache"));

            IHostInteractionInternal hostInteraction = new HostInteraction(settings);

            Directory.CreateDirectory(Path.Combine(workingDir, "jquery"));
            string jqueryFilePath = Path.Combine("jquery", "jquery.min.js");
            File.WriteAllText(Path.Combine(workingDir, jqueryFilePath), "");

            await hostInteraction.DeleteFilesAsync(new[] { jqueryFilePath }, CancellationToken.None);

            Assert.IsFalse(File.Exists(Path.Combine(workingDir, jqueryFilePath)));
            Assert.IsFalse(Directory.Exists(Path.Combine(workingDir, "jquery")));
        }

        [TestMethod]
        public async Task Test_DeleteFilesAsync_DoesNotDeleteNonEmptyFolders()
        {
            string workingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(workingDir);

            EnvironmentSettings settings = TestEnvironmentHelper.GetTestSettings(
                workingDir,
                Path.Combine(workingDir, "cache"));

            IHostInteractionInternal hostInteraction = new HostInteraction(settings);

            Directory.CreateDirectory(Path.Combine(workingDir, "jquery"));
            Directory.CreateDirectory(Path.Combine(workingDir, "jquery", "bootstrap"));
            string jqueryFilePath = Path.Combine("jquery", "jquery.min.js");
            File.WriteAllText(Path.Combine(workingDir, jqueryFilePath), "");

            await hostInteraction.DeleteFilesAsync(new[] { jqueryFilePath }, CancellationToken.None);

            Assert.IsFalse(File.Exists(Path.Combine(workingDir, jqueryFilePath)));
            Assert.IsTrue(Directory.Exists(Path.Combine(workingDir, "jquery")));
        }
    }
}
