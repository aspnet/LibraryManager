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
    [ProvideAutoLoad(PackageGuids.guidUiContextString)]
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
            "ActiveProjectFlavor:{349C5851-65DF-11DA-9384-00065B846F21}",
            "ActiveProjectFlavor:{E24C65DC-7377-472B-9ABA-BC803B73C61A}",
            "ActiveProjectCapability:DotNetCoreWeb",
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
            "ActiveProjectFlavor:{349C5851-65DF-11DA-9384-00065B846F21}",
            "ActiveProjectFlavor:{E24C65DC-7377-472B-9ABA-BC803B73C61A}",
            "ActiveProjectCapability:DotNetCoreWeb" })]

    public sealed class LibraryManagerPackage : AsyncPackage
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
