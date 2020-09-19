// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Web.LibraryManager.Vsix.ErrorList
{
    internal class TableEntriesSnapshot : TableEntriesSnapshotBase
    {
        private readonly string _projectName;

        internal TableEntriesSnapshot(IEnumerable<DisplayError> result, string projectName, string fileName)
        {
            _projectName = projectName;

            Errors = result.ToList();
            Url = fileName;
        }

        public List<DisplayError> Errors { get; }

        public override int VersionNumber { get; } = 1;

        public override int Count
        {
            get { return Errors.Count; }
        }

        public string Url { get; }

        public override bool TryGetValue(int index, string keyName, out object content)
        {
            if (keyName == null)
            {
                throw new System.ArgumentNullException(nameof(keyName));
            }

            if (index < 0 || index >= Errors.Count)
            {
                content = null;
                return false;
            }

            DisplayError error = Errors[index];

            switch (keyName)
            {
                case StandardTableKeyNames.DocumentName:
                    content = Url;
                    return true;
                case StandardTableKeyNames.ErrorCategory:
                    content = vsTaskCategories.vsTaskCategoryMisc;
                    return true;
                case StandardTableKeyNames.Line:
                    content = error.Line;
                    return true;
                case StandardTableKeyNames.Column:
                    content = error.Column;
                    return true;
                case StandardTableKeyNames.Text:
                    content = error.Description;
                    return true;
                case StandardTableKeyNames.ErrorSeverity:
                    content = __VSERRORCATEGORY.EC_ERROR;
                    return true;
                case StandardTableKeyNames.Priority:
                    content = vsTaskPriority.vsTaskPriorityMedium;
                    return true;
                case StandardTableKeyNames.ErrorSource:
                    content = ErrorSource.Other;
                    return true;
                case StandardTableKeyNames.BuildTool:
                    content = Vsix.Name;
                    return true;
                case StandardTableKeyNames.ErrorCode:
                    content = error.ErrorCode;
                    return true;
                case StandardTableKeyNames.ProjectName:
                    content = _projectName;
                    return true;
                case StandardTableKeyNames.HelpLink:
                    content = error.HelpLink;
                    return true;
                default:
                    content = null;
                    return false;
            }
        }
    }
}
