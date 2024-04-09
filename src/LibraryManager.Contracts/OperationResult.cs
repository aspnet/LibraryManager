// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// Represents the outcome of an operation, including an output, a state of completion, and any applicable errors.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class OperationResult<T>
    {
        /// <summary>
        /// Create a new instance of <see cref="OperationResult{T}"/>.
        /// </summary>
        public OperationResult(T? result)
        {
            Errors = new List<IError>();
            Result = result;
        }

        /// <summary>
        /// Create a new instance of an OperationResult with the specified errors but no output.
        /// </summary>
        /// <param name="errors"></param>
        public OperationResult(params IError[] errors)
        {
            Errors = new List<IError>(errors);
        }

        /// <summary>
        /// Creates a new OperationResult with a specified output and errors.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="error"></param>
        public OperationResult(T result, params IError[] error)
        {
            var list = new List<IError>(error);
            Errors = list;
            Result = result;
        }

        /// <summary>
        /// <c>True</c> if the installation was cancelled; otherwise false;
        /// </summary>
        public bool Cancelled { get; set; }

        /// <summary>
        /// <c>True</c> if the install was successful; otherwise <c>False</c>.
        /// </summary>
        /// <remarks>
        /// The value is <c>True</c> if the <see cref="Errors"/> list is empty and the operation was not cancelled.
        /// </remarks>
        public bool Success => !Cancelled && Errors.Count == 0;

        /// <summary>
        /// <c>True</c> if the library is up to date; otherwise <c>False</c>.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public bool UpToDate { get; set; }

        /// <summary>
        /// A list of errors that occurred during the operation.
        /// </summary>
        public IList<IError> Errors { get; }

        /// <summary>
        /// The output of the operation, if any.
        /// </summary>
        public T? Result { get; set; }

        /// <summary>
        /// Create a successful result with the specified output.
        /// </summary>
        public static OperationResult<T> FromSuccess(T output)
        {
            return new OperationResult<T>(output);
        }

        /// <summary>
        /// Create a cancelled outcome, with a specified output if applicable.
        /// </summary>
        public static OperationResult<T> FromCancelled(T? output)
        {
            return new OperationResult<T>(output)
            {
                Cancelled = true
            };
        }

        /// <summary>Create an OperationResult from an error</summary>
        public static OperationResult<T> FromError(IError error)
        {
            return new OperationResult<T>(error);
        }

        /// <summary>Create an up-to-date outcome for the specified output.</summary>
        public static OperationResult<T> FromUpToDate(T output)
        {
            return new OperationResult<T>(output)
            {
                UpToDate = true,
            };
        }
    }
}
