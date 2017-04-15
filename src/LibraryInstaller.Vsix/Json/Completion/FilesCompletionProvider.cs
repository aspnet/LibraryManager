// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryInstaller.Contracts;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Microsoft.Web.LibraryInstaller.Vsix
{
    [Export(typeof(IJSONCompletionListProvider))]
    [Name(nameof(FilesCompletionProvider))]
    internal class FilesCompletionProvider : BaseCompletionProvider
    {
        public override JSONCompletionContextType ContextType
        {
            get { return JSONCompletionContextType.ArrayElement; }
        }

        protected override IEnumerable<JSONCompletionEntry> GetEntries(JSONCompletionContext context)
        {
            JSONMember member = context.ContextItem.FindType<JSONMember>();

            if (member == null || member.UnquotedNameText != "files")
                yield break;

            var parent = member.Parent as JSONObject;

            if (!JsonHelpers.TryGetInstallationState(parent, out ILibraryInstallationState state))
                yield break;

            if (string.IsNullOrEmpty(state.LibraryId))
                yield break;

            var dependencies = Dependencies.FromConfigFile(ConfigFilePath);
            IProvider provider = dependencies.GetProvider(state.ProviderId);
            ILibraryCatalog catalog = provider?.GetCatalog();

            if (catalog == null)
                yield break;

            Task<ILibrary> task = catalog.GetLibraryAsync(state.LibraryId, CancellationToken.None);
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
                yield return new SimpleCompletionEntry(Resources.Text.Loading, KnownMonikers.Loading, context.Session);

                task.ContinueWith((a) =>
                {
                    if (!(task.Result is ILibrary library))
                        return;

                    if (!context.Session.IsDismissed)
                    {
                        var results = new List<JSONCompletionEntry>();

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

            Telemetry.TrackUserTask("completionfiles");
        }

        private static IEnumerable<string> GetUsedFiles(JSONCompletionContext context)
        {
            JSONArray array = context.ContextItem.FindType<JSONArray>();

            if (array == null)
                yield break;

            foreach (JSONArrayElement arrayElement in array.Elements)
            {
                if (arrayElement.Value is JSONTokenItem token && token.Text != context.ContextItem.Text)
                {
                    yield return token.CanonicalizedText;
                }
            }
        }

        private FrameworkElement GetPresenter(JSONCompletionContext context)
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