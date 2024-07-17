// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
