// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using Tasks = System.Threading.Tasks;

namespace LibraryInstaller.Vsix
{
    [Guid(PackageGuids.guidPackageString)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(PackageGuids.guidUiContextString)]
    [ProvideUIContextRule(PackageGuids.guidUiContextString, Vsix.Name,
        "WAP | WebSite | DotNetCoreWeb | ProjectK",// | Cordova | Node",
        new string[] {
            "WAP",
            "WebSite",
            "DotNetCoreWeb",
            "ProjectK",
            "Cordova",
            "Node"
        },
        new string[] {
            "ActiveProjectFlavor:{349C5851-65DF-11DA-9384-00065B846F21}",
            "ActiveProjectFlavor:{E24C65DC-7377-472B-9ABA-BC803B73C61A}",
            "ActiveProjectFlavor:{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}",
            "ActiveProjectCapability:DotNetCoreWeb",
            "ActiveProjectCapability:DependencyPackageManagement",
            "ActiveProjectFlavor:{3AF33F2E-1136-4D97-BBB7-1795711AC8B8}",
        })]
    public sealed class LibraryInstallerPackage : AsyncPackage
    {
        protected override async Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            if (await GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                InstallLibraryCommand.Initialize(this, commandService);
                CleanCommand.Initialize(this, commandService);
                RestoreCommand.Initialize(this, commandService);
                RestoreSolutionCommand.Initialize(this, commandService);
            }
        }
    }
}
