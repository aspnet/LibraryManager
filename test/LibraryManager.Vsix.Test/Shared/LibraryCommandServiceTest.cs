// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.Web.LibraryManager.Vsix.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Shared;
using Moq;

namespace Microsoft.Web.LibraryManager.Vsix.Test.Shared
{
    using Mocks = LibraryManager.Mocks;

    [TestClass]
    public class LibraryCommandServiceTest
    {
        [TestMethod]
        public async Task UninstallAsync_DeletesFilesFromDisk()
        {
            IHostInteraction mockInteraction = new Mocks.HostInteraction();
            var mockTaskStatusCenterService = new Mock<ITaskStatusCenterService>();
            mockTaskStatusCenterService.Setup(m => m.CreateTaskHandlerAsync(It.IsAny<string>()))
                                       .Returns(Task.FromResult(new Mock<ITaskHandler>().Object));
            var testInstallationState = new LibraryInstallationState
            {
                ProviderId = "testProvider",
                Files = new[] { "test.js" },
                DestinationPath = "testDestination",
            };
            Dictionary<string, string> installedFiles = new()
            {
                { Path.Combine(mockInteraction.WorkingDirectory, "testDestination", "test.js"), Path.Combine(mockInteraction.WorkingDirectory, "test.js")}
            };
            var testGoalState = new LibraryInstallationGoalState(testInstallationState, installedFiles);
            var mockDependencies = new Dependencies(mockInteraction, new IProvider[]
            {
                new Mocks.Provider(mockInteraction)
                {
                    Id = "testProvider",
                    Catalog = new Mocks.LibraryCatalog(),
                    Result = new OperationResult<LibraryInstallationGoalState>
                    {
                        Result = testGoalState,
                    },
                    GoalState = testGoalState,
                    SupportsLibraryVersions = true,
                }
            });
            var mockDependenciesFactory = new Mock<IDependenciesFactory>();
            mockDependenciesFactory.Setup(m => m.FromConfigFile(It.IsAny<string>()))
                                   .Returns(mockDependencies);
            LibraryIdToNameAndVersionConverter.Instance.Reinitialize(mockDependencies);

            string manifestContents = @"{
    ""version"": ""1.0"",
    ""libraries"": [
        {
            ""library"": ""test@1.0"",
            ""provider"": ""testProvider"",
            ""destination"": ""testDestination""
        }
    ]
}";
            byte[] manifestBytes = Encoding.Default.GetBytes(manifestContents);

            string configFilePath = Path.Combine(mockInteraction.WorkingDirectory, "libman.json");
            await mockInteraction.WriteFileAsync("libman.json", () => new MemoryStream(manifestBytes), default(LibraryInstallationState), CancellationToken.None);
            await mockInteraction.WriteFileAsync(@"testDestination\test.js", () => new MemoryStream(manifestBytes), default(LibraryInstallationState), CancellationToken.None);
            var solutionEvents = new DefaultSolutionEvents(new Mock<IVsSolution>().Object);

            var ut = new LibraryCommandService(mockDependenciesFactory.Object, mockTaskStatusCenterService.Object, solutionEvents);
            await ut.UninstallAsync(configFilePath, "test", "1.0", "testProvider", CancellationToken.None);

            Assert.IsFalse(File.Exists(Path.Combine(mockInteraction.WorkingDirectory, "testDestination", "test.js")));
        }
    }
}
