using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Test.Apex.Services;
using Microsoft.Test.Apex.VisualStudio.Editor;
using Microsoft.Test.Apex.VisualStudio.Shell;
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

            // It can take a little while for the editor buffer to get updated, so check for it in a loop
            WaitFor.IsTrue(() => Editor.Contents.Trim() == afterUninstall, TimeSpan.FromSeconds(1));
            Helpers.FileIO.WaitForDeletedFile(restoreFolder, Path.Combine(restoreFolder, "jquery.min.js"), true);
        }

        [TestMethod]
        public void UninstallLibrary_RemoveFilesystemLibrary()
        {
            string libraryPath = Path.Combine(SolutionRootPath, "LooseFiles");
            string libraryPathJson = libraryPath.Replace("\\", "\\\\");
            string withLibrary = @"{
  ""version"": ""1.0"",
  ""libraries"": [
    {
      ""library"": """ + libraryPathJson + @""",
      ""provider"": ""filesystem"",
      ""destination"": ""wwwroot/UninstallSuggestedAction/files"",
      ""files"": [ ""filesystem.js"" ]
    }
  ]
}";
            string afterUninstall = @"{
  ""version"": ""1.0"",
  ""libraries"": [
  ]
}";

            SetManifestContents(withLibrary);

            string restoreFolder = Path.Combine(SolutionRootPath, ProjectName, "wwwroot", "UninstallSuggestedAction", "files");
            Helpers.FileIO.WaitForRestoredFile(restoreFolder, Path.Combine(restoreFolder, "filesystem.js"), true);

            Editor.Caret.MoveToExpression(@"""library""");
            Editor.LightBulb.Verify.IsLightBulbPresent(TimeSpan.FromSeconds(5));
            // TODO: Use resource string so that this test can run in non-English VS.
            // Note: VS truncates the file path, so find the Uninstall action based on prefix.
            Editor.LightBulb.GetActiveLightBulb().Actions.Single(a => a.Text.StartsWith("Uninstall")).Invoke();

            // It can take a little while for the editor buffer to get updated, so check for it in a loop
            WaitFor.IsTrue(() => Editor.Contents.Trim() == afterUninstall, TimeSpan.FromSeconds(1));
            Helpers.FileIO.WaitForDeletedFile(restoreFolder, Path.Combine(restoreFolder, "filesystem.js"), true);
        }
    }
}
