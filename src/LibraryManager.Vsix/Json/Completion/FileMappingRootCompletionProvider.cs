// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Contracts;
using Microsoft.WebTools.Languages.Json.Editor.Completion;
using Microsoft.WebTools.Languages.Json.Parser.Nodes;

namespace Microsoft.Web.LibraryManager.Vsix.Json.Completion
{
    [Export(typeof(IJsonCompletionListProvider))]
    [Name(nameof(FileMappingRootCompletionProvider))]
    internal class FileMappingRootCompletionProvider : BaseCompletionProvider
    {
        private readonly IDependenciesFactory _dependenciesFactory;

        [ImportingConstructor]
        internal FileMappingRootCompletionProvider(IDependenciesFactory dependenciesFactory)
        {
            _dependenciesFactory = dependenciesFactory;
        }

        public override JsonCompletionContextType ContextType => JsonCompletionContextType.PropertyValue;

        [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Checked completion first")]
        protected override IEnumerable<JsonCompletionEntry> GetEntries(JsonCompletionContext context)
        {
            MemberNode member = context.ContextNode.FindType<MemberNode>();

            // This provides completions for libraries/[n]/fileMappings/[m]/root
            if (member == null || member.UnquotedNameText != ManifestConstants.Root)
                yield break;

            MemberNode possibleFileMappingsNode = member.Parent.FindType<MemberNode>();
            bool isInFileMapping = possibleFileMappingsNode?.UnquotedNameText == ManifestConstants.FileMappings;
            if (!isInFileMapping)
                yield break;

            ObjectNode parent = possibleFileMappingsNode.Parent as ObjectNode;

            if (!JsonHelpers.TryGetInstallationState(parent, out ILibraryInstallationState state))
                yield break;

            if (string.IsNullOrEmpty(state.Name))
                yield break;

            IDependencies dependencies = _dependenciesFactory.FromConfigFile(ConfigFilePath);
            IProvider provider = dependencies.GetProvider(state.ProviderId);
            ILibraryCatalog catalog = provider?.GetCatalog();

            if (catalog is null)
            {
                yield break;
            }

            Task<ILibrary> task = catalog.GetLibraryAsync(state.Name, state.Version, CancellationToken.None);

            if (task.IsCompleted)
            {
                if (task.Result is ILibrary library)
                {
                    foreach (JsonCompletionEntry item in GetRootCompletions(context, library))
                    {
                        yield return item;
                    }
                }
            }
            else
            {
                yield return new SimpleCompletionEntry(Resources.Text.Loading, string.Empty, KnownMonikers.Loading, context.Session);
                _ = task.ContinueWith(async (t) =>
                {
                    await VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    if (!(t.Result is ILibrary library))
                        return;

                    if (!context.Session.IsDismissed)
                    {
                        IEnumerable<JsonCompletionEntry> completions = GetRootCompletions(context, library);

                        UpdateListEntriesSync(context, completions);
                    }
                }, TaskScheduler.Default);
            }
        }

        private IEnumerable<JsonCompletionEntry> GetRootCompletions(JsonCompletionContext context, ILibrary library)
        {
            HashSet<string> libraryFolders = [];
            foreach (string file in library.Files.Keys)
            {
                int sepIndex = file.LastIndexOf('/');
                if (sepIndex >= 0)
                {
                    libraryFolders.Add(file.Substring(0, file.LastIndexOf('/')));
                }
            }

            return libraryFolders.Select(folder => new SimpleCompletionEntry(folder, KnownMonikers.FolderClosed, context.Session));
        }
    }
}
