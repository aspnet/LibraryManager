// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.LibraryManager.Vsix.Contracts;
using Tasks = System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Vsix
{
    [Guid(PackageGuids.guidPackageString)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
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
        [Import]
        internal ILibraryCommandService LibraryCommandService { get; set; }

        [Import]
        internal IDependenciesFactory DependenciesFactory { get; private set; }

        protected override async Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IComponentModel componentModel = GetService(typeof(SComponentModel)) as IComponentModel;
            componentModel.DefaultCompositionService.SatisfyImportsOnce(this);

            OleMenuCommandService commandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            if (commandService != null && LibraryCommandService != null)
            {
                InstallLibraryCommand.Initialize(commandService, LibraryCommandService, DependenciesFactory);
                CleanCommand.Initialize(this, commandService, LibraryCommandService);
                RestoreCommand.Initialize(this, commandService, LibraryCommandService);
                RestoreSolutionCommand.Initialize(this, commandService, LibraryCommandService);
                RestoreOnBuildCommand.Initialize(this, commandService, DependenciesFactory);
                ManageLibrariesCommand.Initialize(this, commandService, LibraryCommandService, DependenciesFactory);
            }
        }
    }
}
