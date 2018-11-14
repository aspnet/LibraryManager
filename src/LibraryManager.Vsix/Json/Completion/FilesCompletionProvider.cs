// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.WebTools.Languages.Json.Editor.Completion;
using Microsoft.WebTools.Languages.Json.Parser.Nodes;
using Microsoft.WebTools.Languages.Shared.Parser.Nodes;

namespace Microsoft.Web.LibraryManager.Vsix
{
    [Export(typeof(IJsonCompletionListProvider))]
    [Name(nameof(FilesCompletionProvider))]
    internal class FilesCompletionProvider : BaseCompletionProvider
    {
        public override JsonCompletionContextType ContextType
        {
            get { return JsonCompletionContextType.ArrayElement; }
        }

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

            var dependencies = Dependencies.FromConfigFile(ConfigFilePath);
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
                        ImageSource glyph = WpfUtil.GetIconForFile(presenter, file, out bool isThemeIcon);
                        yield return new SimpleCompletionEntry(file, glyph, context.Session);
                    }
                }
            }
            else
            {
                yield return new SimpleCompletionEntry(Resources.Text.Loading, string.Empty, KnownMonikers.Loading, context.Session);

                task.ContinueWith((a) =>
                {
                    if (!(task.Result is ILibrary library))
                        return;

                    if (!context.Session.IsDismissed)
                    {
                        var results = new List<JsonCompletionEntry>();

                        foreach (string file in library.Files.Keys)
                        {
                            if (!usedFiles.Contains(file))
                            {
                                ImageSource glyph = WpfUtil.GetIconForFile(presenter, file, out bool isThemeIcon);
                                results.Add(new SimpleCompletionEntry(file, glyph, context.Session));
                            }
                        }

                        UpdateListEntriesSync(context, results);
                    }
                });
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
