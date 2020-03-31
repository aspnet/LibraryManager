// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Shared;
using Microsoft.WebTools.Languages.Json.Editor.Completion;
using Microsoft.WebTools.Languages.Json.Parser.Nodes;
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

            if (member == null || member.UnquotedNameText != "files")
                yield break;

            var parent = member.Parent as ObjectNode;

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

            if (task.IsCompleted)
            {
                if (!(task.Result is ILibrary library))
                    yield break;

                foreach (string file in library.Files.Keys)
                {
                    if (!usedFiles.Contains(file))
                    {
                        ImageMoniker glyph = WpfUtil.GetImageMonikerForFile(file);
                        yield return new SimpleCompletionEntry(file, glyph, context.Session);
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
                        var results = new List<JsonCompletionEntry>();

                        foreach (string file in library.Files.Keys)
                        {
                            if (!usedFiles.Contains(file))
                            {
                                ImageMoniker glyph = WpfUtil.GetImageMonikerForFile(file);
                                results.Add(new SimpleCompletionEntry(file, glyph, context.Session));
                            }
                        }

                        UpdateListEntriesSync(context, results);
                    }
                }, TaskScheduler.Default);
            }
        }

        private static IEnumerable<string> GetUsedFiles(JsonCompletionContext context)
        {
            ArrayNode array = context.ContextNode.FindType<ArrayNode>();

            if (array == null)
                yield break;

            foreach (ArrayElementNode arrayElement in array.Elements)
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
    }
}
