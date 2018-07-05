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
            var telResult = new Dictionary<string, object>();
            telResult.Add("LibrariesCount", results.Count());
            telResult.Add($@"{operation}_time", Math.Round(elapsedTime.TotalSeconds, 2));

            foreach (ILibraryOperationResult result in results.Where(r => r.Success && !r.UpToDate))
            {
                telResult.Add($@"{result.InstallationState.LibraryId}_Success", new TelemetryPiiProperty(result.InstallationState.LibraryId));

                if (result.InstallationState.ProviderId != null)
                {
                    telResult.TryGetValue($@"{result.InstallationState.ProviderId}_Success", out object count);
                    telResult[$@"{result.InstallationState.ProviderId}_Success"] = count != null ? (int)count + 1 : 1;
                }

            }

            foreach (ILibraryOperationResult result in results.Where(r => r.Errors.Any()))
            {
                telResult.Add($@"{result.InstallationState.LibraryId}_Failure", new TelemetryPiiProperty(result.InstallationState.LibraryId));

                if (result.InstallationState.ProviderId != null)
                {
                    telResult.TryGetValue($@"{result.InstallationState.ProviderId}_Failure", out object count);
                    telResult[$@"{result.InstallationState.ProviderId}_Failure"] = count != null ? (int)count + 1 : 1;
                }

                foreach (IError error in result.Errors)
                {
                    TrackOperation("error", TelemetryResult.Failure, new KeyValuePair<string, object>("code", error.Code));
                }
            }

            foreach (ILibraryOperationResult result in results.Where(r => r.UpToDate))
            {
                telResult.Add($@"{result.InstallationState.LibraryId}_Uptodate", new TelemetryPiiProperty(result.InstallationState.LibraryId));

                if (result.InstallationState.ProviderId != null)
                {
                    telResult.TryGetValue($@"{result.InstallationState.ProviderId}_Uptodate", out object count);
                    telResult[$@"{result.InstallationState.ProviderId}_Uptodate"] = count != null ? (int)count + 1 : 1;
                }
            }

            foreach (ILibraryOperationResult result in results.Where(r => r.Cancelled))
            {
                telResult.Add($@"{result.InstallationState.LibraryId}_Cancelled", new TelemetryPiiProperty(result.InstallationState.LibraryId));

                if (result.InstallationState.ProviderId != null)
                {
                    telResult.TryGetValue($@"{result.InstallationState.ProviderId}_Cancelled", out object count);
                    telResult[$@"{result.InstallationState.ProviderId}_Cancelled"] = count != null ? (int)count + 1 : 1;
                }
            }

            TrackUserTask($@"{operation}_Operation", TelemetryResult.None, telResult.Select(i => new KeyValuePair<string, object>(i.Key, i.Value)).ToArray());
        }
    }
}
