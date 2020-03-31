// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
