using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Test.Apex.Editor;
using Microsoft.Test.Apex.VisualStudio.Editor;
using Omni.Common;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    public class LibmanTestsUtility
    {
        public static void WaitForCompletionEntries(IVisualStudioTextEditorTestExtension editor, IEnumerable<string> expectedCompletionEntries, bool caseInsensitive, int timeout = 1000)
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
