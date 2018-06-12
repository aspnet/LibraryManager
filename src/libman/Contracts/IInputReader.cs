// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Web.LibraryManager.Tools
{
    /// <summary>
    /// Provides a way to get input from user.
    /// </summary>
    public interface IInputReader
    {
        /// <summary>
        /// Prompts the user for an input for the <paramref name="fieldName"/>
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        string GetUserInput(string fieldName);

        /// <summary>
        /// Prompts the user for an input for the <paramref name="fieldName"/>
        /// with a suggested default <paramref name="defaultValue"/>
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        string GetUserInputWithDefault(string fieldName, string defaultValue);
    }
}
