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
            telResult.Add("ErrorCodes", string.Join(":", generalErrorCodes));

            IEnumerable<string> providers = results.Select(r => r.InstallationState?.ProviderId).Distinct();

            foreach (string provider in providers)
            {
                IEnumerable<ILibraryOperationResult> providerResults = results.Where(r => r.InstallationState?.ProviderId == provider);
                IEnumerable<ILibraryOperationResult> successfulProviderResults = providerResults.Where(r => r.Success && !r.UpToDate);

                List<string> providerErrorCodes = GetErrorCodes(providerResults);

                List<TelemetryPiiProperty> librariesNames_Success = GetPiiLibrariesNames(successfulProviderResults);
                List<TelemetryPiiProperty> librariesNames_Failure = GetPiiLibrariesNames(providerResults.Where(r => r.Errors.Any()));
                List<TelemetryPiiProperty> librariesNames_Cancelled = GetPiiLibrariesNames(providerResults.Where(r => r.Cancelled));
                List<TelemetryPiiProperty> librariesNames_Uptodate = GetPiiLibrariesNames(providerResults.Where(r => r.UpToDate));

                telResult.Add($"ErrorCodes_{provider}", string.Join(":", providerErrorCodes));

                if (librariesNames_Success.Count > 0)
                {
                    telResult[$"LibrariesCount_{provider}_Success"] = librariesNames_Success.Count;
                    telResult[$"LibrariesIDs_{provider}_Success"] = string.Join(":", librariesNames_Success);
                    telResult[$"{provider}_LibrariesFilesCount"] = GetProviderLibraryFilesCount(successfulProviderResults);
                }

                if (librariesNames_Failure.Count > 0)
                {
                    telResult[$"LibrariesCount_{provider}_Failure"] = librariesNames_Failure.Count;
                    telResult[$"LibrariesIDs_{provider}_Failure"] = string.Join(":", librariesNames_Failure);
                }

                if (librariesNames_Cancelled.Count > 0)
                {
                    telResult[$"LibrariesCount_{provider}_Cancelled"] = librariesNames_Cancelled.Count;
                    telResult[$"LibrariesIDs_{provider}_Cancelled"] = string.Join(":", librariesNames_Cancelled);
                }

                if (librariesNames_Uptodate.Count > 0)
                {
                    telResult[$"LibrariesCount_{provider}_Uptodate"] = librariesNames_Uptodate.Count;
                    telResult[$"LibrariesIDs_{provider}_Uptodate"] = string.Join(":", librariesNames_Uptodate);
                }

            }

            TrackUserTask($@"{operation}_Operation", TelemetryResult.None, telResult.Select(i => new KeyValuePair<string, object>(i.Key, i.Value)).ToArray());
        }

        private static int GetProviderLibraryFilesCount(IEnumerable<ILibraryOperationResult> successfulProviderResults)
        {
            int count = 0;

            foreach (ILibraryOperationResult result in successfulProviderResults)
            {
                if (result.InstallationState != null)
                {
                    count += result.InstallationState.Files.Count();
                }
            }

            return count;
        }

        internal static void LogErrors(string eventName, IEnumerable<ILibraryOperationResult> results)
        {
            List<string> errorCodes = GetErrorCodes(results.Where(r => r.Errors.Any()));
            TrackUserTask(eventName, TelemetryResult.Failure, new KeyValuePair<string, object>("Errorcode", string.Join(":", errorCodes)));
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

        private static List<TelemetryPiiProperty> GetPiiLibrariesNames(IEnumerable<ILibraryOperationResult> results)
        {
            List<TelemetryPiiProperty> librariesNames = new List<TelemetryPiiProperty>();

            foreach (ILibraryOperationResult result in results)
            {
                librariesNames.Add(new TelemetryPiiProperty(result.InstallationState?.Name));
            }

            return librariesNames;
        }
    }
}
