using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Reflection;
using System.Linq;

namespace Microsoft.Web.LibraryManager.Build.Test
{
    [TestClass]
    public class RestoreTaskTest
    {
        private string _projectFolder;
        private RestoreTask _task;
        private MockEngine _buildEngine;

        [TestInitialize]
        public void Setup()
        {
            _projectFolder = Path.Combine(Path.GetTempPath(), "LibraryManagerBuild");
            _buildEngine = new MockEngine();
            string path = typeof(Manifest).GetTypeInfo().Assembly.Location;

            _task = new RestoreTask()
            {
                ProjectDirectory = _projectFolder,
                FileName = Path.Combine(_projectFolder, "libman.json"),
                BuildEngine = _buildEngine,
                ProviderAssemblies = new[] { new TaskItem { ItemSpec = path } }
            };

            Directory.CreateDirectory(_projectFolder);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(_projectFolder, true);
        }

        [TestMethod]
        public void Execute_NoManifest()
        {
            bool success = _task.Execute();

            Assert.IsTrue(success);
            Assert.AreEqual(1, _buildEngine.Warnings.Count);
            Assert.AreEqual(0, _buildEngine.Messages.Count);
            Assert.AreEqual(1, _task.ProviderAssemblies.Length);
            Assert.IsNull(_task.FilesWritten);
        }

        [TestMethod]
        public void Execute_PartiallyValidManifest()
        {
            string path = Path.Combine(_projectFolder, _task.FileName);
            File.WriteAllText(path, _doc);

            bool success = _task.Execute();

            Assert.IsFalse(success);
            Assert.AreEqual(0, _buildEngine.Warnings.Count);
            Assert.AreEqual(2, _buildEngine.Messages.Count);
            Assert.AreEqual(1, _buildEngine.Errors.Count);
            Assert.AreEqual("LIB002", _buildEngine.Errors.First().Code);
            Assert.IsNull(_task.FilesWritten);
        }

        [TestMethod]
        public void Execute_EmptyManifest()
        {
            string path = Path.Combine(_projectFolder, _task.FileName);
            File.WriteAllText(path, "{}");

            bool succuess = _task.Execute();

            Assert.IsFalse(succuess);
            Assert.AreEqual(1, _buildEngine.Errors.Count);
            Assert.AreEqual(2, _buildEngine.Messages.Count);
            Assert.IsNull(_task.FilesWritten);
        }

        [TestMethod]
        public void Execute_MalformedManifest()
        {
            string path = Path.Combine(_projectFolder, _task.FileName);
            File.WriteAllText(path, "invalid json");

            bool succuess = _task.Execute();

            Assert.IsFalse(succuess);
            Assert.AreEqual(0, _buildEngine.Warnings.Count);
            Assert.AreEqual(2, _buildEngine.Messages.Count);
            Assert.AreEqual(1, _buildEngine.Errors.Count);
            Assert.IsNull(_task.FilesWritten);
        }

        private string _doc = $@"{{
  ""{ManifestConstants.Version}"": ""1.0"",
  ""{ManifestConstants.Libraries}"": [
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.1.1"",
      ""{ManifestConstants.Provider}"": ""cdnjs"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""jquery.js"", ""jquery.min.js"" ]
    }},
    {{
      ""{ManifestConstants.Library}"": ""../path/to/file.txt"",
      ""{ManifestConstants.Provider}"": ""filesystem"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""file.txt"" ]
    }}
  ]
}}
";
    }
}
