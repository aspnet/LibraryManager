using System;
using System.Collections.Generic;
using Microsoft.Test.Apex.Editor;
using Microsoft.Test.Apex.VisualStudio.Editor;
using Omni.Common;

namespace Microsoft.Web.LibraryManager.IntegrationTest.Helpers
{
    public class CompletionHelper
    {
        public void WaitForCompletionEntries(IVisualStudioTextEditorTestExtension editor, IEnumerable<string> expectedCompletionEntries, bool caseInsensitive, int timeout = 1000)
        {
            string errorMessage = WaitForCompletionEntriesHelper(editor, expectedCompletionEntries, caseInsensitive, timeout);

            if (errorMessage != null)
            {
                string newErrorMessage = WaitForCompletionEntriesHelper(editor, expectedCompletionEntries, caseInsensitive, timeout);

                if (newErrorMessage != null)
                {
                    errorMessage = String.Concat(errorMessage, "\r\nFailed even when forcing completion with double timeout");
                }
                else
                {
                    errorMessage = String.Concat(errorMessage, "\r\n*Didn't* fail when forcing completion with double timeout");
                }

                throw new TimeoutException(errorMessage);
            }
        }

        public void WaitForCompletionEntry(IVisualStudioTextEditorTestExtension editor, string expectedEntry, bool caseInsensitive, int timeout = 1000)
        {
            WaitForCompletionEntries(editor, new[] { expectedEntry }, caseInsensitive, timeout);
        }

        public void WaitForCompletionEntryNotPresent(IVisualStudioTextEditorTestExtension editor, string entryText, bool caseInsensitive, int timeout = 1000)
        {
            string errorMessage = WaitForCompletionEntriesHelper(editor, new[] { entryText }, caseInsensitive, timeout);

            if (errorMessage == null)
            {
                errorMessage = entryText + " is presented in the completion list.";
            }

            if (!errorMessage.Contains("Timed out waiting for completion entry: "))
            {
                throw new TimeoutException(errorMessage);
            }
        }

        public CompletionList WaitForCompletionItems(IVisualStudioTextEditorTestExtension editor, int timeout = 1000)
        {
            CompletionList items = null;

            WaitFor.TryIsTrue(() =>
            {
                try
                {
                    IVisualStudioCompletionListTestExtension completionList = editor.Intellisense.GetActiveCompletionList();

                    // Make another call if completion list is not available or is still loading.
                    if (completionList == null || completionList.Items.Count == 1 && completionList.Items[0].Text.Equals(Vsix.Resources.Text.Loading))
                    {
                        return false;
                    }

                    items = completionList.Items;

                    return true;
                }
                catch (EditorException)
                {
                    return false;
                }
            }, TimeSpan.FromMilliseconds(timeout), TimeSpan.FromMilliseconds(500));

            return items;
        }

        private static string WaitForCompletionEntriesHelper(IVisualStudioTextEditorTestExtension editor, IEnumerable<string> expectedCompletionEntries, bool caseInsensitive, int timeout)
        {
            string errorMessage = null;
            WaitFor.TryIsTrue(() =>
            {
                try
                {
                    IVisualStudioCompletionListTestExtension completionList = editor.Intellisense.GetActiveCompletionList();

                    if (completionList == null)
                    {
                        errorMessage = "Completion list not present.";
                        return false;
                    }

                    HashSet<string> comparisonSet = caseInsensitive ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) : new HashSet<string>();
                    foreach (CompletionItem item in completionList.Items)
                    {
                        comparisonSet.Add(item.Text);
                    }

                    // Make another call if it's still loading.
                    if (comparisonSet.Count == 1 && comparisonSet.Contains(Vsix.Resources.Text.Loading))
                    {
                        errorMessage = "Completion list not present.";
                        return false;
                    }

                    errorMessage = null;
                    if (expectedCompletionEntries != null)
                    {
                        foreach (string curEntry in expectedCompletionEntries)
                        {
                            if (!comparisonSet.Contains(curEntry))
                            {
                                errorMessage = String.Concat(errorMessage, "\r\nTimed out waiting for completion entry: ", curEntry, ".");
                            }
                        }

                        // Do not force another call if we already got the whole completion list.
                        return true;
                    }
                }
                catch (EditorException exc)
                {
                    errorMessage = exc.ToString();
                }

                return (errorMessage == null);
            }, TimeSpan.FromMilliseconds(timeout), TimeSpan.FromMilliseconds(500));

            return errorMessage;
        }
    }
}
