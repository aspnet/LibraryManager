// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Vsix.UI.Converters;

namespace Microsoft.Web.LibraryManager.Vsix.Test.UI.Converters
{
    [TestClass]
    public class WatermarkVisibilityConverterTests
    {
        [DataTestMethod]
        [DataRow(null, Visibility.Visible)]
        [DataRow("", Visibility.Visible)]
        [DataRow("non-empty", Visibility.Hidden)]
        public void Convert_StringValues(string input, Visibility expected)
        {
            var testObj = new WatermarkVisibilityConverter();

            var result = (Visibility)testObj.Convert(input, null, null, null);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Convert_NonStringValue_ShouldReturnHidden()
        {
            var testObj = new WatermarkVisibilityConverter();
            string[] input = Array.Empty<string>(); // non-string, non-null object

            var result = (Visibility)testObj.Convert(input, null, null, null);

            Assert.AreEqual(Visibility.Hidden, result);
        }
    }
}
