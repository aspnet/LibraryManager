using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Mocks;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class FileHelperTest
    {
        private string _projectFilePath;
        private string _cacheFolder;
        private string _projectFolder;
        private string _cacheFilePath;

        [TestInitialize]
        public void Setup()
        {
            _cacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");
            _projectFolder = Path.Combine(Path.GetTempPath(), "LibraryManager");
            _projectFilePath = Path.Combine(_projectFolder, "projectFile{0}");
            _cacheFilePath = Path.Combine(_cacheFolder, "cacheFile{0}");

            Directory.CreateDirectory(_projectFolder);
        }

        [TestCleanup]
        public void Cleanup()
        {
            TestUtils.DeleteDirectoryWithRetries(_projectFolder);
        }

        [TestMethod]
        public void AsyncWriteAndReadToCacheFileDoesNotThrow()
        {
            // Arrange
            List<Thread> threads = new List<Thread>();
            Random rnd = new Random();
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            Mocks.WebRequestHandler _requestHandler = new Mocks.WebRequestHandler();

            Debug.WriteLine("Starting test");

            for (int i = 0; i < 10; i++)
            {
                Thread thread = new Thread(async delegate ()
                {
                    for (int j = 0; j < 100; j++)
                    {
                        int next = rnd.Next(1, 100);

                        if (next % 2 == 0)
                        {
                            Stream content = await _requestHandler.GetStreamAsync("FakeUrl", token);
                            string fileName = string.Format(_cacheFilePath, rnd.Next(1, 100));
                            await FileHelpers.WriteToFileAsync(fileName, content, token);
                            Debug.WriteLine(string.Format("Thread: {0} => Wrote to file {1}", i, fileName));
                        }
                        else
                        {
                            string fileName = string.Format(_cacheFilePath, rnd.Next(1, 100));
                            if (File.Exists(fileName))
                            {
                                await FileHelpers.ReadFileTextAsync(fileName, token);
                                Debug.WriteLine(string.Format("Thread: {0} => Read from file {1}", i, fileName));
                            }
                        }
                    }

                    for (int j = 0; j < 100; j++)
                    {
                        int next = rnd.Next(1, 100);

                        if (next % 2 == 0)
                        {
                            Stream content = await _requestHandler.GetStreamAsync("FakeUrl", token);
                            string fileName = string.Format(_projectFilePath, rnd.Next(1, 100));
                            await FileHelpers.WriteToFileAsync(fileName, content, token);
                            Debug.WriteLine(string.Format("Thread: {0} => Wrote to file {1}", i, fileName));
                        }
                        else
                        {
                            string fileName = string.Format(_projectFilePath, rnd.Next(1, 100));
                            await FileHelpers.ReadFileTextAsync(fileName, token);
                            Debug.WriteLine(string.Format("Thread: {0} => Read from file {1}", i, fileName));
                        }
                    }
                });

                threads.Add(thread);
                thread.Start();
            }

            for (int i = 0; i < threads.Count; i++)
            {
                threads[i].Join();
            }

            //Verify
            Assert.IsTrue(true);
        }
              
    }
}
