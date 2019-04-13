using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            string restoreFolder = Path.Combine(SolutionRootPath, _projectName, "wwwroot", "UninstallSuggestedAction", "files");
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
