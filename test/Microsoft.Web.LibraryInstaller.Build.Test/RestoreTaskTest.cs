using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Reflection;
using System.Linq;

namespace Microsoft.Web.LibraryInstaller.Build.Test
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
            _projectFolder = Path.Combine(Path.GetTempPath(), "LibraryInstallerBuild");
            _buildEngine = new MockEngine();
            string path = typeof(Manifest).GetTypeInfo().Assembly.Location;

            _task = new RestoreTask()
            {
                ProjectDirectory = _projectFolder,
                FileName = Path.Combine(_projectFolder, "library.json"),
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
            _task.Execute();

            Assert.AreEqual(1, _buildEngine.Warnings.Count);
        }

        [TestMethod]
        public void Execute_PartiallyValidManifest()
        {
            string path = Path.Combine(_projectFolder, _task.FileName);
            File.WriteAllText(path, _doc);

            _task.Execute();

            Assert.AreEqual(1, _buildEngine.Warnings.Count);
            Assert.AreEqual("LIB002", _buildEngine.Warnings.First().Code);
            Assert.AreEqual(2, _task.FilesWritten.Length);
        }

        private const string _doc = @"{
  ""version"": ""1.0"",
  ""packages"": [
    {
      ""id"": ""jquery@3.1.1"",
      ""provider"": ""cdnjs"",
      ""path"": ""lib"",
      ""files"": [ ""jquery.js"", ""jquery.min.js"" ]
    },
    {
      ""id"": ""../path/to/file.txt"",
      ""provider"": ""filesystem"",
      ""path"": ""lib"",
      ""files"": [ ""file.txt"" ]
    }
  ]
}
";
    }
}
