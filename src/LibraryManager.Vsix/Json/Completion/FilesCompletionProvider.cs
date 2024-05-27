// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Shared;
using Microsoft.WebTools.Languages.Json.Editor.Completion;
using Microsoft.WebTools.Languages.Json.Parser.Nodes;
using Microsoft.WebTools.Languages.Shared.Parser;
using Microsoft.WebTools.Languages.Shared.Parser.Nodes;

namespace Microsoft.Web.LibraryManager.Vsix.Json.Completion
{
    [Export(typeof(IJsonCompletionListProvider))]
    [Name(nameof(FilesCompletionProvider))]
    internal class FilesCompletionProvider : BaseCompletionProvider
    {
        private readonly IDependenciesFactory _dependenciesFactory;

        [ImportingConstructor]
        internal FilesCompletionProvider(IDependenciesFactory dependenciesFactory)
        {
            _dependenciesFactory = dependenciesFactory;
        }

        public override JsonCompletionContextType ContextType
        {
            get { return JsonCompletionContextType.ArrayElement; }
        }

        [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Checked for task completion before calling .Result")]
        protected override IEnumerable<JsonCompletionEntry> GetEntries(JsonCompletionContext context)
        {
            MemberNode member = context.ContextNode.FindType<MemberNode>();

            // We can show completions for "files".  This could be libraries/[n]/files or
            // libraries/[n]/fileMappings/[m]/files.
            if (member == null || member.UnquotedNameText != "files")
                yield break;

            // If the current member is "files", then it is either:
            // - a library "files" property
            // - a fileMapping "files" property
            MemberNode possibleFileMappingsNode = member.Parent.FindType<MemberNode>();
            bool isFileMapping = possibleFileMappingsNode?.UnquotedNameText == "fileMappings";

            ObjectNode parent = isFileMapping
                ? possibleFileMappingsNode.Parent as ObjectNode
                : member.Parent as ObjectNode;

            if (!JsonHelpers.TryGetInstallationState(parent, out ILibraryInstallationState state))
                yield break;

            if (string.IsNullOrEmpty(state.Name))
                yield break;

            IDependencies dependencies = _dependenciesFactory.FromConfigFile(ConfigFilePath);
            IProvider provider = dependencies.GetProvider(state.ProviderId);
            ILibraryCatalog catalog = provider?.GetCatalog();

            if (catalog == null)
                yield break;

            Task<ILibrary> task = catalog.GetLibraryAsync(state.Name, state.Version, CancellationToken.None);
            FrameworkElement presenter = GetPresenter(context);
            IEnumerable<string> usedFiles = GetUsedFiles(context);

            string rootPathPrefix = isFileMapping ? GetRootValue(member) : string.Empty;
            static string GetRootValue(MemberNode fileMappingNode)
            {
                FindFileMappingRootVisitor visitor = new FindFileMappingRootVisitor();
                fileMappingNode.Parent?.Accept(visitor);
                return visitor.FoundNode?.UnquotedValueText ?? string.Empty;
            }

            if (task.IsCompleted)
            {
                if (!(task.Result is ILibrary library))
                    yield break;

                IEnumerable<JsonCompletionEntry> completions = GetFileCompletions(context, usedFiles, library, rootPathPrefix);
                foreach (JsonCompletionEntry item in completions)
                {
                    yield return item;
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
                        IEnumerable<JsonCompletionEntry> completions = GetFileCompletions(context, usedFiles, library, rootPathPrefix);

                        UpdateListEntriesSync(context, completions);
                    }
                }, TaskScheduler.Default);
            }
        }

        private static IEnumerable<JsonCompletionEntry> GetFileCompletions(JsonCompletionContext context, IEnumerable<string> usedFiles, ILibrary library, string root)
        {
            static bool alwaysInclude(string s) => true;
            bool includeIfUnderRoot(string s) => FileHelpers.IsUnderRootDirectory(s, root);

            Func<string, bool> filter = string.IsNullOrEmpty(root)
                ? alwaysInclude
                : includeIfUnderRoot;

            bool rootHasTrailingSlash = string.IsNullOrEmpty(root) || root.EndsWith("/") || root.EndsWith("\\");
            int nameOffset = rootHasTrailingSlash ? root.Length : root.Length + 1;

            foreach (string file in library.Files.Keys)
            {
                if (filter(file))
                {
                    string fileSubPath = file.Substring(nameOffset);
                    if (!usedFiles.Contains(fileSubPath))
                    {
                        ImageMoniker glyph = WpfUtil.GetImageMonikerForFile(file);
                        yield return new SimpleCompletionEntry(fileSubPath, glyph, context.Session);
                    }
                }
            }
        }

        private static IEnumerable<string> GetUsedFiles(JsonCompletionContext context)
        {
            ArrayNode array = context.ContextNode.FindType<ArrayNode>();

            if (array == null)
                yield break;

            foreach (ArrayElementNode arrayElement in array.ElementNodes)
            {
                if (arrayElement.Value is TokenNode token && token.Text != context.ContextNode.GetText())
                {
                    yield return token.GetCanonicalizedText();
                }
            }
        }

        private FrameworkElement GetPresenter(JsonCompletionContext context)
        {
            var presenter = context?.Session?.Presenter as FrameworkElement;

            presenter?.SetBinding(ImageThemingUtilities.ImageBackgroundColorProperty, new Binding("Background")
            {
                Source = presenter,
                Converter = new BrushToColorConverter()
            });

            return presenter;
        }

        private class FindFileMappingRootVisitor : INodeVisitor
        {
            public MemberNode FoundNode { get; private set; }

            public VisitNodeResult Visit(Node node)
            {
                if (node is ObjectNode)
                {
                    return VisitNodeResult.Continue;
                }
                // we only look at the object and it's members, this is not a recursive search
                if (node is not MemberNode mn)
                {
                    return VisitNodeResult.SkipChildren;
                }

                if (mn.UnquotedNameText == ManifestConstants.Root)
                {
                    FoundNode = mn;
                    return VisitNodeResult.Cancel;
                }

                return VisitNodeResult.SkipChildren;
            }
        }
    }
}
