// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
[assembly: InternalsVisibleTo("Microsoft.Web.LibraryManager.Vsix.IntegrationTest")]
[assembly: InternalsVisibleTo("Microsoft.Web.LibraryManager.Vsix.Test")]

// TODO: Setting the NeutralResourcesLanguage attribute breaks our menu commands.
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1824:Mark assemblies with NeutralResourcesLanguagesAttribute", Justification = "Currently doing this causes our menu items (commands) to not load.")]
//[assembly: NeutralResourcesLanguage("en")]
