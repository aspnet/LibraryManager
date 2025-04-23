// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Test.TestUtilities;

public static class AssertExtensions
{
    public static void ErrorsEqual(this Assert assert, IList<IError> expected, IList<IError> actual)
    {
        string BuildString(IList<IError> errors)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (IError error in errors)
            {
                stringBuilder.AppendLine($"{error.Code}: {error.Message}");
            }
            return stringBuilder.ToString();
        }

        Assert.AreEqual(BuildString(expected), BuildString(actual));
    }
}
