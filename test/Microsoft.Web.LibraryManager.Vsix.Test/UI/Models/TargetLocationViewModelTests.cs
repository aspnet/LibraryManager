// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Vsix.Search;
using Microsoft.Web.LibraryManager.Vsix.Test.Mocks;
using Microsoft.Web.LibraryManager.Vsix.UI.Models;

namespace Microsoft.Web.LibraryManager.Vsix.Test.UI.Models
{
    [TestClass]
    public class TargetLocationViewModelTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_NullLibraryNameBinding_ShouldThrow()
        {
            new TargetLocationViewModel("", null, new LocationSearchService(new LibraryManager.Mocks.HostInteraction()));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_NullSearchService_ShouldThrow()
        {
            new TargetLocationViewModel("", new LibraryNameBinding(), null);
        }

        [TestMethod]
        public void LibraryNameChanged_StartFromInitial_ShouldAppendNewName()
        {
            var binding = new LibraryNameBinding();
            var testObj = new TargetLocationViewModel("initial/", binding, new NullSearchService());

            binding.LibraryName = "NewName";

            Assert.AreEqual("initial/NewName/", testObj.SearchText);
        }

        [TestMethod]
        public void LibraryNameChanged_NewNameIsNullOrEmpty_ShouldNotChange()
        {
            var binding = new LibraryNameBinding();
            var testObj = new TargetLocationViewModel("initial/", binding, new NullSearchService());

            binding.LibraryName = "NewName";

            binding.LibraryName = "";
            Assert.AreEqual("initial/NewName/", testObj.SearchText);

            binding.LibraryName = null;

            Assert.AreEqual("initial/NewName/", testObj.SearchText);
        }

        [TestMethod]
        public void LibraryNameChanged_UpdateExistingName_ShouldReplace()
        {
            var binding = new LibraryNameBinding();
            var testObj = new TargetLocationViewModel("initial/", binding, new NullSearchService());
            binding.LibraryName = "FirstName";

            binding.LibraryName = "SecondName";

            Assert.AreEqual("initial/SecondName/", testObj.SearchText);
        }
    }
}
