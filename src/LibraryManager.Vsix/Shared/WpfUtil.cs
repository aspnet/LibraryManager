// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Web.LibraryManager.Vsix.Shared
{
    internal static class WpfUtil
    {
        public static ImageMoniker GetImageMonikerForFile(string file)
        {
            var imageService = ServiceProvider.GlobalProvider.GetService(typeof(SVsImageService)) as IVsImageService2;

            if (imageService == null)
            {
                return default;
            }

            return imageService.GetImageMonikerForFile(file);
        }
    }
}
