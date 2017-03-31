// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSON.Editor.Document;

namespace LibraryInstaller.Vsix
{
    class SuggestedActionsSource : ISuggestedActionsSource
    {
        private ITextView _view;
        private JSONEditorDocument _doc;

        public SuggestedActionsSource(ITextView view, ITextBuffer buffer)
        {
            _view = view;
            _doc = JSONEditorDocument.TryFromTextBuffer(buffer);
        }

        public async Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            return await Task.Factory.StartNew(() =>
            {



                return false;// _section != null;
            });
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            var list = new List<SuggestedActionSet>();

            //if (_section != null)
            //{
            //    var removeDuplicate = new RemoveDuplicatePropertiesAction(_section, _view);
            //    if (removeDuplicate.IsEnabled)
            //        list.AddRange(CreateActionSet(removeDuplicate));

            //    var sortProperties = new SortPropertiesAction(_section, _view);
            //    var sortAllProperties = new SortAllPropertiesAction(_document, _view);
            //    list.AddRange(CreateActionSet(sortProperties, sortAllProperties));

            //    var deleteSection = new DeleteSectionAction(range.Snapshot.TextBuffer, _section);
            //    list.AddRange(CreateActionSet(deleteSection));

            //    // Suppressions
            //    IEnumerable<ParseItem> items = _document.ItemsInSpan(range).Where(p => p.HasErrors);
            //    if (items.Any())
            //    {
            //        IEnumerable<DisplayError> errors = items.SelectMany(i => i.Errors);
            //        var actions = new List<SuppressErrorAction>();

            //        foreach (DisplayError error in errors)
            //        {
            //            var action = new SuppressErrorAction(_document, error.Name);

            //            if (action.IsEnabled)
            //                actions.Add(action);
            //        }

            //        list.AddRange(CreateActionSet(actions.ToArray()));
            //    }
            //}

            return list;
        }

        //public IEnumerable<SuggestedActionSet> CreateActionSet(params BaseSuggestedAction[] actions)
        //{
        //    return new[] { new SuggestedActionSet(actions) };
        //}

        public void Dispose()
        {
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            // This is a sample provider and doesn't participate in LightBulb telemetry
            telemetryId = Guid.Empty;
            return false;
        }


        public event EventHandler<EventArgs> SuggestedActionsChanged
        {
            add { }
            remove { }
        }
    }
}
