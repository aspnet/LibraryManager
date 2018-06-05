using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TaskStatusCenter;

namespace Microsoft.Web.LibraryManager.Vsix
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
        ITaskHandler CreateTaskHandler(string title);
    }
}
