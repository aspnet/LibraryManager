// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TaskStatusCenter;

/// <summary>
/// Implementation of the TaskStatusCenterService allowing backaground tasks to be registered to IVsTaskStatusCenterService.
/// </summary>
namespace Microsoft.Web.LibraryManager.Vsix.Shared
{
    using Package = Microsoft.VisualStudio.Shell.Package;

    [Export(typeof(ITaskStatusCenterService))]
    internal class TaskStatusCenterService : ITaskStatusCenterService
    {
        public async Task<ITaskHandler> CreateTaskHandlerAsync(string title)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsTaskStatusCenterService taskStatusCenter = (IVsTaskStatusCenterService)Package.GetGlobalService(typeof(SVsTaskStatusCenterService));
            TaskHandlerOptions options = default(TaskHandlerOptions);
            options.Title = title;
            options.ActionsAfterCompletion = CompletionActions.None;

            TaskProgressData data = default(TaskProgressData);
            data.CanBeCanceled = true;

            ITaskHandler handler = taskStatusCenter.PreRegister(options, data);
            return handler;
        }
    }
}
