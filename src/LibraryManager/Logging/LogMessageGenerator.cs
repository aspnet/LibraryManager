// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;

namespace Microsoft.Web.LibraryManager.Logging
{
    /// <summary>
    /// Helper class for getting strings for output to CLI and VS
    /// </summary>
    public static class LogMessageGenerator
    {
        /// <summary>
        /// Gets a partial success string for the given operation.
        /// </summary>
        public static string GetPartialSuccessString(OperationType operation,
            int successfulResults,
            int failedResults,
            int cancelledResults,
            int uptodateResults,
            TimeSpan timeSpan)
        {
            string successString = Resources.Text.Restore_NumberOfLibrariesSucceeded;
            string failedString = Resources.Text.Restore_NumberOfLibrariesFailed;
            string cancelledString = Resources.Text.Restore_NumberOfLibrariesCancelled;
            string uptodateString = Resources.Text.Restore_NumberOfLibrariesUptodate;

            // Partial success only applies to Restore and Clean operations.
            // All other bulk operations are treated as restore.
            if (operation == OperationType.Clean)
            {
                successString = Resources.Text.Clean_NumberOfLibrariesSucceeded;
                failedString = Resources.Text.Clean_NumberOfLibrariesFailed;
                cancelledString = Resources.Text.Clean_NumberOfLibrariesCancelled;
            }

            string message = string.Empty;
            message = successfulResults > 0 ? message + string.Format(successString, successfulResults, Math.Round(timeSpan.TotalSeconds, 2)) + Environment.NewLine : message;
            message = failedResults > 0 ? message + string.Format(failedString, failedResults) + Environment.NewLine : message;
            message = cancelledResults > 0 ? message + string.Format(cancelledString, cancelledResults) + Environment.NewLine : message;
            message = uptodateResults > 0 ? message + string.Format(uptodateString, uptodateResults) + Environment.NewLine : message;

            return message;
        }

        /// <summary>
        /// Gets all success string for the given operaton.
        /// </summary>
        public static string GetAllSuccessString(OperationType operation, int totalCount, TimeSpan timeSpan, string libraryId)
        {
            switch (operation)
            {
                case OperationType.Restore:
                    return string.Format(LibraryManager.Resources.Text.Restore_NumberOfLibrariesSucceeded, totalCount, Math.Round(timeSpan.TotalSeconds, 2));

                case OperationType.Clean:
                    return string.Format(LibraryManager.Resources.Text.Clean_NumberOfLibrariesSucceeded, totalCount, Math.Round(timeSpan.TotalSeconds, 2));

                case OperationType.Uninstall:
                    return string.Format(LibraryManager.Resources.Text.Uninstall_LibrarySucceeded, libraryId ?? string.Empty);

                case OperationType.Upgrade:
                    return string.Format(LibraryManager.Resources.Text.Update_LibrarySucceeded, libraryId ?? string.Empty);

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Gets all failure string for the given operation.
        /// </summary>
        public static string GetAllFailuresString(OperationType operation, int totalCount, string libraryId)
        {
            switch (operation)
            {
                case OperationType.Restore:
                    return string.Format(LibraryManager.Resources.Text.Restore_NumberOfLibrariesFailed, totalCount);

                case OperationType.Clean:
                    return string.Format(LibraryManager.Resources.Text.Clean_NumberOfLibrariesFailed, totalCount);

                case OperationType.Uninstall:
                    return string.Format(LibraryManager.Resources.Text.Uninstall_LibraryFailed, libraryId ?? string.Empty);

                case OperationType.Upgrade:
                    return string.Format(LibraryManager.Resources.Text.Update_LibraryFailed, libraryId ?? string.Empty);

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Gets all canceled string for the given operation.
        /// </summary>
        public static string GetAllCancelledString(OperationType operation, int totalCount, TimeSpan timeSpan, string libraryId)
        {
            switch (operation)
            {
                case OperationType.Restore:
                    return string.Format(LibraryManager.Resources.Text.Restore_NumberOfLibrariesCancelled, totalCount, Math.Round(timeSpan.TotalSeconds, 2));

                case OperationType.Clean:
                    return string.Format(LibraryManager.Resources.Text.Clean_NumberOfLibrariesCancelled, totalCount, Math.Round(timeSpan.TotalSeconds, 2));

                case OperationType.Uninstall:
                    return string.Format(LibraryManager.Resources.Text.Uninstall_LibraryCancelled, libraryId ?? string.Empty);

                case OperationType.Upgrade:
                    return string.Format(LibraryManager.Resources.Text.Update_LibraryCancelled, libraryId ?? string.Empty);

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Gets operation header string.
        /// </summary>
        public static string GetOperationHeaderString(OperationType operationType, string libraryId)
        {
            switch (operationType)
            {
                case OperationType.Restore:
                    return Resources.Text.Restore_OperationStarted;

                case OperationType.Clean:
                    return Resources.Text.Clean_OperationStarted;

                case OperationType.Uninstall:
                    return string.Format(Resources.Text.Uninstall_LibraryStarted, libraryId ?? string.Empty);

                case OperationType.Upgrade:
                    return string.Format(Resources.Text.Update_LibraryStarted, libraryId ?? string.Empty);

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Gets the summary header string for an operation.
        /// </summary>
        public static string GetSummaryHeaderString(OperationType operationType)
        {
            switch (operationType)
            {
                case OperationType.Restore:
                    return Resources.Text.Restore_OperationCompleted;

                case OperationType.Clean:
                    return Resources.Text.Clean_OperationCompleted;

                case OperationType.Uninstall:
                    return Resources.Text.Uninstall_OperationCompleted;

                case OperationType.Upgrade:
                    return Resources.Text.Upgrade_OperationCompleted;

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Gets the summary header string for an errored operation/ failed operation.
        /// </summary>
        public static string GetErrorsHeaderString(OperationType operationType)
        {
            switch (operationType)
            {
                case OperationType.Restore:
                    return Resources.Text.Restore_OperationCompletedWithErrors;

                case OperationType.Clean:
                    return Resources.Text.Clean_OperationCompletedWithErrors;

                case OperationType.Uninstall:
                    return Resources.Text.Uninstall_OperationCompletedWithErrors;

                case OperationType.Upgrade:
                    return Resources.Text.Upgrade_OperationCompletedWithErrors;

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Gets the operation summary string based on number of successful and failure operations.
        /// </summary>
        public static string GetOperationSummaryString(IEnumerable<ILibraryOperationResult> results, OperationType operation, TimeSpan elapsedTime)
        {
            if (results != null && results.Any())
            {
                int totalResultsCounts = results.Count();
                IEnumerable<ILibraryOperationResult> successfulResults = results.Where(r => r.Success && !r.UpToDate);
                IEnumerable<ILibraryOperationResult> failedResults = results.Where(r => r.Errors.Any());
                IEnumerable<ILibraryOperationResult> cancelledRessults = results.Where(r => r.Cancelled);
                IEnumerable<ILibraryOperationResult> upToDateResults = results.Where(r => r.UpToDate);

                bool allSuccess = successfulResults.Count() == totalResultsCounts;
                bool allFailed = failedResults.Count() == totalResultsCounts;
                bool allCancelled = cancelledRessults.Count() == totalResultsCounts;
                bool allUpToDate = upToDateResults.Count() == totalResultsCounts;

                string messageText = string.Empty;
                string libraryId = string.Empty;

                if (allUpToDate)
                {
                    messageText = LibraryManager.Resources.Text.Restore_LibrariesUptodate + Environment.NewLine;
                }
                else if (allSuccess)
                {
                    libraryId = GetLibraryId(results, operation);
                    messageText = LogMessageGenerator.GetAllSuccessString(operation, totalResultsCounts, elapsedTime, libraryId);
                }
                else if (allCancelled)
                {
                    libraryId = GetLibraryId(results, operation);
                    messageText = LogMessageGenerator.GetAllCancelledString(operation, totalResultsCounts, elapsedTime, libraryId);
                }
                else if (allFailed)
                {
                    libraryId = GetLibraryId(results, operation);
                    messageText = LogMessageGenerator.GetAllFailuresString(operation, totalResultsCounts, libraryId);
                }
                else
                {
                    messageText = LogMessageGenerator.GetPartialSuccessString(operation,
                        successfulResults.Count(), failedResults.Count(), cancelledRessults.Count(), upToDateResults.Count(), elapsedTime);
                }

                return messageText;
            }

            return string.Empty;
        }

        private static string GetLibraryId(IEnumerable<ILibraryOperationResult> totalResults, OperationType operation)
        {
            if (operation == OperationType.Uninstall || operation == OperationType.Upgrade)
            {
                if (totalResults != null && totalResults.Count() == 1)
                {
                    ILibraryInstallationState state = totalResults.First().InstallationState;
                    return LibraryIdToNameAndVersionConverter.Instance.GetLibraryId(state.Name, state.Version, state.ProviderId);
                }
            }

            return string.Empty;
        }
    }
}
