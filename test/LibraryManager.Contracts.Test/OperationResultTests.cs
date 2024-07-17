// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Web.LibraryManager.Contracts;

namespace LibraryManager.Contracts.Test
{
    [TestClass]
    public class OperationResultTests
    {
        [TestMethod]
        public void HasResult_NoErrors_Success()
        {
            var sut = new OperationResult<object>(new object());

            Assert.IsTrue(sut.Success);
            Assert.IsFalse(sut.Cancelled);
            Assert.IsNotNull(sut.Result);
            Assert.AreEqual(0, sut.Errors.Count);
            Assert.IsFalse(sut.UpToDate);
        }

        [TestMethod]
        public void HasResult_WithErrors_Failure()
        {
            var sut = new OperationResult<object>(new Error("TEST", "Test error"))
            {
                Result = new object(),
            };

            Assert.IsFalse(sut.Success);
            Assert.IsFalse(sut.Cancelled);
            Assert.IsNotNull(sut.Result);
            Assert.AreEqual(1, sut.Errors.Count);
            Assert.AreEqual("TEST", sut.Errors[0].Code);
        }

        [TestMethod]
        public void NoResult_NoErrors_Failure()
        {
            // This scenario may represent an unknown failure - no errors were reported,
            // but the expected result was not returned, so the operation was not successful.

            var sut = new OperationResult<object>((object?)null);

            Assert.IsFalse(sut.Success);
            Assert.IsFalse(sut.Cancelled);
            Assert.IsNull(sut.Result);
            Assert.AreEqual(0, sut.Errors.Count);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Cancelled_NoErrors_Failure(bool isResultNull)
        {
            var sut = new OperationResult<object>(isResultNull ? null : new object())
            {
                Cancelled = true,
            };

            Assert.IsFalse(sut.Success);
            Assert.IsTrue(sut.Cancelled);
            Assert.AreEqual(isResultNull, sut.Result is null);
            Assert.AreEqual(0, sut.Errors.Count);
        }
    }
}
