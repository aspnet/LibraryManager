// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal static class Telemetry
    {
        private const string _namespace = Constants.TelemetryNamespace;

        public static void TrackUserTask(string name, TelemetryResult result = TelemetryResult.None, params KeyValuePair<string, object>[] properties)
        {
            string actualName = name.Replace(" ", "_");

            var task = new UserTaskEvent(_namespace + actualName, result);

            foreach (KeyValuePair<string, object> property in properties)
            {
                task.Properties.Add(property);
            }

            TelemetryService.DefaultSession.PostEvent(task);
        }

        public static void TrackOperation(string name, TelemetryResult result = TelemetryResult.None, params KeyValuePair<string, object>[] properties)
        {
            string actualName = name.Replace(" ", "_");
            var task = new OperationEvent(_namespace + actualName, result);

            foreach (KeyValuePair<string, object> property in properties)
            {
                task.Properties.Add(property);
            }

            TelemetryService.DefaultSession.PostEvent(task);
        }

        public static void TrackException(string name, Exception exception)
        {
            if (string.IsNullOrWhiteSpace(name) || exception == null)
                return;

            string actualName = name.Replace(" ", "_");
            TelemetryService.DefaultSession.PostFault(_namespace + actualName, exception.Message, exception);
        }

        internal static void LogEventsSummary(IEnumerable<ILibraryOperationResult> results, OperationType operation, TimeSpan elapsedTime)
        {
            double elapsedTimeRounded = Math.Round(elapsedTime.TotalSeconds, 2);
            string elapsedTimeStr = elapsedTimeRounded.ToString(System.Globalization.CultureInfo.InvariantCulture);
            List<string> generalErrorCodes = GetErrorCodes(results.Where(r => r.InstallationState == null && r.Errors.Any()));

            Dictionary<string, object> telResult = new Dictionary<string, object>();
            telResult.Add("LibrariesCount", results.Count());
            telResult.Add($"{operation}_time", elapsedTimeStr);
            telResult.Add("ErrorCodes", string.Join(" :", generalErrorCodes));

            IEnumerable<string> providers = results.Select(r => r.InstallationState?.ProviderId).Distinct();

            foreach (string provider in providers)
            {
                IEnumerable<ILibraryOperationResult> providerResults = results.Where(r => r.InstallationState?.ProviderId == provider);
                List<string> providerErrorCodes = GetErrorCodes(providerResults);

                List<string> librariesNames_Success = GetLibrariesNames(providerResults.Where(r => r.Success && !r.UpToDate));
                List<string> librariesNames_Failure = GetLibrariesNames(providerResults.Where(r => r.Errors.Any()));
                List<string> librariesNames_Cancelled = GetLibrariesNames(providerResults.Where(r => r.Cancelled));
                List<string> librariesNames_Uptodate = GetLibrariesNames(providerResults.Where(r => r.UpToDate));

                telResult.Add($"ErrorCodes_{provider}", string.Join(" :", providerErrorCodes));

                if (librariesNames_Success.Count > 0)
                {
                    telResult[$"LibrariesCount_{provider}_Success"] = librariesNames_Success.Count;
                    telResult[$"LibrariesIDs_{provider}_Success"] = new TelemetryPiiProperty(string.Join(" :", librariesNames_Success));
                }

                if (librariesNames_Failure.Count > 0)
                {
                    telResult[$"LibrariesCount_{provider}_Failure"] = librariesNames_Failure.Count;
                    telResult[$"LibrariesIDs_{provider}_Failure"] = new TelemetryPiiProperty(string.Join(" :", librariesNames_Failure));
                }

                if (librariesNames_Cancelled.Count > 0)
                {
                    telResult[$"LibrariesCount_{provider}_Cancelled"] = librariesNames_Cancelled.Count;
                    telResult[$"LibrariesIDs_{provider}_Cancelled"] = new TelemetryPiiProperty(string.Join(" :", librariesNames_Cancelled));
                }

                if (librariesNames_Uptodate.Count > 0)
                {
                    telResult[$"LibrariesCount_{provider}_Uptodate"] = librariesNames_Uptodate.Count;
                    telResult[$"LibrariesIDs_{provider}_Uptodate"] = new TelemetryPiiProperty(string.Join(" :", librariesNames_Uptodate));
                }

                foreach (ILibraryOperationResult result in providerResults)
                {
                    if (result.InstallationState?.Files != null)
                    {
                        telResult[$"{provider}_LibraryFilesCount"] = result.InstallationState.Files.Count;
                    }
                }
            }

            TrackUserTask($@"{operation}_Operation", TelemetryResult.None, telResult.Select(i => new KeyValuePair<string, object>(i.Key, i.Value)).ToArray());
        }

        internal static void LogErrors(string eventName, IEnumerable<ILibraryOperationResult> results)
        {
            List<string> errorCodes = GetErrorCodes(results.Where(r => r.Errors.Any()));
            TrackUserTask(eventName, TelemetryResult.Failure, new KeyValuePair<string, object>("Errorcode", string.Join(" :", errorCodes)));
        }

        private static List<string> GetErrorCodes(IEnumerable<ILibraryOperationResult> results)
        {
            List<string> errorCodes = new List<string>();

            foreach (ILibraryOperationResult result in results)
            {
                errorCodes.AddRange(result.Errors.Select(e => e.Code));
            }

            return errorCodes;
        }

        private static List<string> GetLibrariesNames(IEnumerable<ILibraryOperationResult> results)
        {
            List<string> librariesNames = new List<string>();

            foreach (ILibraryOperationResult result in results)
            {
                librariesNames.Add(result.InstallationState?.LibraryId);
            }

            return librariesNames;
        }
    }
}
