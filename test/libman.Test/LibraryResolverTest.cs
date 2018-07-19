// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Tools.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Test
{
    [TestClass]
    public class LibraryResolverTest : CommandTestBase
    {
        [TestInitialize]
        public override void Setup()
        {
            base.Setup();
            _dependencies = new Dependencies(HostEnvironment);
        }

        [TestMethod]
        public async Task TestResolveLibraryAsync()
        {
            string libmanjsonPath = Path.Combine(WorkingDir, "libman.json");
            File.WriteAllText(libmanjsonPath, _manifestContents);

            Manifest manifest = await Manifest.FromFileAsync(
                libmanjsonPath,
                _dependencies,
                CancellationToken.None);

            // Matches jquery for all providers.
            IReadOnlyList<ILibraryInstallationState> result = LibraryResolver.Resolve(
                "jquery",
                manifest,
                null);

            Assert.AreEqual(3, result.Count);

            Assert.AreEqual("jquery", result[0].Name);
            Assert.AreEqual("", result[0].Version);
            Assert.AreEqual("jquery", result[1].Name);
            Assert.AreEqual("3.3.1", result[1].Version);
            Assert.AreEqual("jquery", result[2].Name);
            Assert.AreEqual("2.2.0", result[2].Version);

            // Matches jquery for cdnjs provider
            result = LibraryResolver.Resolve(
                "jquery",
                manifest,
                _dependencies.GetProvider("cdnjs"));

            Assert.AreEqual(2, result.Count);

            Assert.AreEqual("jquery", result[0].Name);
            Assert.AreEqual("3.3.1", result[0].Version);
            Assert.AreEqual("jquery", result[1].Name);
            Assert.AreEqual("2.2.0", result[1].Version);

            // Matches only one result.
            result = LibraryResolver.Resolve(
                "jquery@3.3.1",
                manifest,
                null);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("jquery", result[0].Name);
            Assert.AreEqual("3.3.1", result[0].Version);

            // Does not match library for a different provider.
            result = LibraryResolver.Resolve(
                "jquery@3.3.1",
                manifest,
                _dependencies.GetProvider("filesystem"));

            Assert.AreEqual(0, result.Count);

            // Does not return partial matches.
            result = LibraryResolver.Resolve(
                "jquery@3.3",
                manifest,
                null);

            Assert.AreEqual(0, result.Count);

            result = LibraryResolver.Resolve(
                "jquer",
                manifest,
                null);

            Assert.AreEqual(0, result.Count);

        }

        [TestMethod]
        public async Task TestResolveByUserChoiceAsync()
        {
            string libmanjsonPath = Path.Combine(WorkingDir, "libman.json");
            File.WriteAllText(libmanjsonPath, _manifestContents);

            Manifest manifest = await Manifest.FromFileAsync(
                libmanjsonPath,
                _dependencies,
                CancellationToken.None);

            var inputReader = HostEnvironment.InputReader as TestInputReader;

            string outputStr = "Select an option:\r\n-----------------\r\n1. {jquery@3.3.1, cdnjs, wwwroot}\r\n2. {jquery@2.2.0, cdnjs, lib}\r\n3. {jquery, filesystem, wwwroot}";

            inputReader.Inputs[outputStr] = "1";

            ILibraryInstallationState result = LibraryResolver.ResolveLibraryByUserChoice(manifest.Libraries, HostEnvironment);

            Assert.AreEqual("jquery", result.Name);
            Assert.AreEqual("3.3.1", result.Version);
        }

        private string _manifestContents = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": [
    {
      ""library"": ""jquery@3.3.1"",
      ""files"": [ ""jquery.min.js"", ""jquery.js"" ]
    },
    {
      ""library"": ""jquery@2.2.0"",
      ""destination"": ""lib""
    },
    {
      ""library"": ""jquery"",
      ""provider"": ""filesystem""
    }
  ]
}";
        private Dependencies _dependencies;
    }
}
