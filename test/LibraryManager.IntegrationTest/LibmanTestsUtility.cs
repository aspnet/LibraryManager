using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Test.Apex.Editor;
using Microsoft.Test.Apex.VisualStudio.Editor;
using Omni.Common;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    public class LibmanTestsUtility
    {
        private static HashSet<string> GetTopLevelDirectoriesAndFiles(string cwd, bool caseInsensitive)
        {
            HashSet<string> topLevelItems = caseInsensitive ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) : new HashSet<string>();
            var dir = new DirectoryInfo(cwd);

            if(dir.Exists)
            {
                foreach(FileSystemInfo item in dir.EnumerateDirectories())
                {
                    topLevelItems.Add(item.Name);
                }
                foreach(FileSystemInfo item in dir.EnumerateFiles())
                {
                    topLevelItems.Add(item.Name);
                }
            }

            return topLevelItems;
        }

        public static void WaitForRestoredFiles(string cwd, IEnumerable<string> expectedFilesAndFolders, bool caseInsensitive, int timeout = 10000)
        {
            string errorMessage = WaitForRestoredFilesHelper(cwd, expectedFilesAndFolders, caseInsensitive, timeout);

            if (errorMessage != null)
            {
                string newErrorMessage = WaitForRestoredFilesHelper(cwd, expectedFilesAndFolders, caseInsensitive, timeout);

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

        private static string WaitForRestoredFilesHelper(string cwd, IEnumerable<string> expectedFilesAndFolders, bool caseInsensitive, int timeout)
        {
            string errorMessage = null;

            WaitFor.TryIsTrue(() =>
            {
                try
                {
                    HashSet<string> topLevelItems = GetTopLevelDirectoriesAndFiles(cwd, caseInsensitive);

                    if (topLevelItems.Count == 0)
                    {
                        errorMessage = "Restore failed for the library.";
                        return false;
                    }

                    errorMessage = null;
                    if (expectedFilesAndFolders != null)
                    {
                        foreach(string item in expectedFilesAndFolders)
                        {
                            if (!topLevelItems.Contains(item))
                            {
                                errorMessage = string.Concat(errorMessage, "\r\nTimed out waiting for: ", item, ".");
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    errorMessage = exc.ToString();
                }

                return errorMessage == null;
            }, TimeSpan.FromMilliseconds(timeout), TimeSpan.FromMilliseconds(500));

            return errorMessage;
        }

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

        public static void WaitForCompletionEntry(IVisualStudioTextEditorTestExtension editor, string expectedEntry, bool caseInsensitive, int timeout = 1000)
        {
            WaitForCompletionEntries(editor, new[] { expectedEntry }, caseInsensitive, timeout);
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
