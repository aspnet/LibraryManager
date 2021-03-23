// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix.Shared
{
    internal static class Telemetry
    {
        private const string Namespace = Constants.TelemetryNamespace;

        public static void TrackUserTask(string name, TelemetryResult result = TelemetryResult.None, params KeyValuePair<string, object>[] properties)
        {
            string actualName = name.Replace(" ", "_");

            var task = new UserTaskEvent(Namespace + actualName, result);

            foreach (KeyValuePair<string, object> property in properties)
            {
                task.Properties.Add(property);
            }

            TelemetryService.DefaultSession.PostEvent(task);
        }

        public static void TrackOperation(string name, TelemetryResult result = TelemetryResult.None, params KeyValuePair<string, object>[] properties)
        {
            string actualName = name.Replace(" ", "_");
            var task = new OperationEvent(Namespace + actualName, result);

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
            TelemetryService.DefaultSession.PostFault(Namespace + actualName, exception.Message, exception);
        }

        internal static void LogEventsSummary(IEnumerable<ILibraryOperationResult> results, OperationType operation, TimeSpan elapsedTime)
        {
            Dictionary<string, object> telResult = new Dictionary<string, object>();
            double elapsedTimeRounded = Math.Round(elapsedTime.TotalSeconds, 2);
            string elapsedTimeStr = elapsedTimeRounded.ToString(System.Globalization.CultureInfo.InvariantCulture);
            List<string> generalErrorCodes = GetErrorCodes(results.Where(r => r.InstallationState == null && r.Errors.Any()));
            IEnumerable<string> providers = results.Select(r => r.InstallationState?.ProviderId).Distinct(StringComparer.OrdinalIgnoreCase);

            telResult.Add("LibrariesCount", results.Count());
            telResult.Add($"{operation}_time", elapsedTimeStr);

            if (generalErrorCodes.Count > 0)
            {
                telResult.Add("ErrorCodes", string.Join(":", generalErrorCodes));
            }

            foreach (string provider in providers)
            {
                List<ILibraryOperationResult> successfulProviderResults = new List<ILibraryOperationResult>();
                List<ILibraryOperationResult> failedProviderResults = new List<ILibraryOperationResult>();
                List<ILibraryOperationResult> cancelledProviderResults = new List<ILibraryOperationResult>();
                List<ILibraryOperationResult> uptodateProviderResults = new List<ILibraryOperationResult>();

                foreach (ILibraryOperationResult result in results)
                {
                    if (result.InstallationState != null &&
                        result.InstallationState.ProviderId.Equals(provider, StringComparison.OrdinalIgnoreCase))
                    {
                        if (result.Success && !result.UpToDate)
                        {
                            successfulProviderResults.Add(result);
                        }
                        else if (result.Errors.Any())
                        {
                            failedProviderResults.Add(result);
                        }
                        else if (result.UpToDate)
                        {
                            uptodateProviderResults.Add(result);
                        }
                        else if (result.Cancelled)
                        {
                            cancelledProviderResults.Add(result);
                        }
                    }
                }

                if (successfulProviderResults.Count > 0)
                {
                    telResult[$"LibrariesCount_{provider}_Success"] = successfulProviderResults.Count;
                }

                if (failedProviderResults.Count > 0)
                {
                    List<string> providerErrorCodes = GetErrorCodes(failedProviderResults);

                    telResult[$"LibrariesCount_{provider}_Failure"] = failedProviderResults.Count;
                    telResult.Add($"ErrorCodes_{provider}", string.Join(":", providerErrorCodes));
                }

                if (cancelledProviderResults.Count > 0)
                {
                    telResult[$"LibrariesCount_{provider}_Cancelled"] = cancelledProviderResults.Count;
                }

                if (uptodateProviderResults.Count > 0)
                {
                    telResult[$"LibrariesCount_{provider}_Uptodate"] = uptodateProviderResults.Count;
                }
            }

            TrackUserTask($@"{operation}_Operation", TelemetryResult.None, telResult.Select(i => new KeyValuePair<string, object>(i.Key, i.Value)).ToArray());
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
    }
}
