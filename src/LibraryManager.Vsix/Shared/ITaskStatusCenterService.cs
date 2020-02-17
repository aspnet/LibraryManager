// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TaskStatusCenter;

namespace Microsoft.Web.LibraryManager.Vsix.Shared
{
    /// <summary>
    /// Provides an interface for a Task Status Center Service
    /// </summary>
    internal interface ITaskStatusCenterService
    {
        /// <summary>
        /// Returns an ITaskHandler with a given task title
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        Task<ITaskHandler> CreateTaskHandlerAsync(string title);
    }
}
