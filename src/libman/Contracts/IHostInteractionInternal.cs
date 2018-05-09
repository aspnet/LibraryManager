using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Contracts
{
    interface IHostInteractionInternal : IHostInteraction
    {
        void UpdateWorkingDirectory(string directory);
        void DeleteFiles(params string[] relativeFilePaths);
    }
}
