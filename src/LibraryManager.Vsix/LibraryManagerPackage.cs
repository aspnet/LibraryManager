// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using Tasks = System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Vsix
{
    [Guid(PackageGuids.guidPackageString)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideUIContextRule(PackageGuids.guidUiContextConfigFileString,
        name: "ConfigFile",
        expression: "(WAP | WebSite | DotNetCoreWeb ) & Config",
        termNames: new string[] {
            "WAP",
            "WebSite",
            "DotNetCoreWeb",
            "Config"
        },
        termValues: new string[] {
            "ActiveProjectFlavor:" + Constants.WAP,
            "ActiveProjectFlavor:" + Constants.WebsiteProject,
            "ActiveProjectCapability:" + Constants.DotNetCoreWebCapability,
            "HierSingleSelectionName:" + Constants.ConfigFileName + "$" })]
    [ProvideUIContextRule(PackageGuids.guidUiContextString, 
        name: Vsix.Name,
        expression: "(WAP | WebSite | DotNetCoreWeb )",
        termNames: new string[] {
            "WAP",
            "WebSite",
            "DotNetCoreWeb"
        },
        termValues: new string[] {
            "ActiveProjectFlavor:" + Constants.WAP,
            "ActiveProjectFlavor:" + Constants.WebsiteProject,
            "ActiveProjectCapability:" + Constants.DotNetCoreWebCapability })]

    internal sealed class LibraryManagerPackage : AsyncPackage
    {
        protected override async Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            if (await GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(false) is OleMenuCommandService commandService)
            {
#if UI_ENABLED
                InstallLibraryCommand.Initialize(this, commandService);
#endif
                CleanCommand.Initialize(this, commandService);
                RestoreCommand.Initialize(this, commandService);
                RestoreSolutionCommand.Initialize(this, commandService);
                RestoreOnBuildCommand.Initialize(this, commandService);
                ManageLibrariesCommand.Initialize(this, commandService);
            }
        }
    }
}
