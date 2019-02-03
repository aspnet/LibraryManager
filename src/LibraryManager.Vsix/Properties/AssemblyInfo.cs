// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Web.LibraryManager.Vsix;

[assembly: AssemblyTitle(Vsix.Name)]
[assembly: AssemblyDescription(Vsix.Description)]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(Vsix.Author)]
[assembly: AssemblyProduct(Vsix.Name)]
[assembly: AssemblyCopyright(Vsix.Author)]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("Microsoft.Web.LibraryManager.IntegrationTest")]
[assembly: InternalsVisibleTo("Microsoft.Web.LibraryManager.Vsix.Test")]

// TODO: Setting the NeutralResourcesLanguage attribute breaks our menu commands.
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1824:Mark assemblies with NeutralResourcesLanguagesAttribute", Justification = "Currently doing this causes our menu items (commands) to not load.")]
//[assembly: NeutralResourcesLanguage("en")]
