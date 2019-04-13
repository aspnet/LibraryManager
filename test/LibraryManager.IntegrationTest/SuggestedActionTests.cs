// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    [TestClass]
    public class SuggestedActionTests : VisualStudioLibmanHostTest
    {
        [TestMethod]
        public void UninstallLibrary_RemovesFilesFromProject()
        {
            string withLibrary = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""libraries"": [
    {
      ""library"": ""jquery@3.3.1"",
      ""destination"": ""wwwroot/UninstallSuggestedAction/files"",
      ""files"": [ ""jquery.min.js"" ]
    }
  ]
}";
            string afterUninstall = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""libraries"": [
  ]
}";

            SetManifestContents(withLibrary);

            string restoreFolder = Path.Combine(SolutionRootPath, ProjectName, "wwwroot", "UninstallSuggestedAction", "files");
            Helpers.FileIO.WaitForRestoredFile(restoreFolder, Path.Combine(restoreFolder, "jquery.min.js"), true);

            Editor.Caret.MoveToExpression("jquery");
            Editor.LightBulb.Verify.IsLightBulbPresent(TimeSpan.FromSeconds(5));
            // TODO: Use resource string so that this test can run in non-English VS.
            Editor.LightBulb.GetActiveLightBulb().InvokeByText("Uninstall jquery@3.3.1");

            Verify.Strings.AreEqual(afterUninstall, Editor.Contents.Trim());
            Helpers.FileIO.WaitForDeletedFile(restoreFolder, Path.Combine(restoreFolder, "jquery.min.js"), true);
        }
    }
}
