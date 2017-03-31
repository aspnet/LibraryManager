using System;
using Microsoft.VisualStudio.Telemetry;

namespace LibraryInstaller.Vsix
{
    public static class Telemetry
    {
        private const string _namespace = Constants.TelemetryNamespace;

        public static void TrackUserTask(string name, TelemetryResult result = TelemetryResult.Success)
        {
            string actualName = name.Replace(" ", "_");
            TelemetryService.DefaultSession.PostUserTask(_namespace + actualName, result);
        }

        public static void TrackOperation(string name, TelemetryResult result = TelemetryResult.Success)
        {
            string actualName = name.Replace(" ", "_");
            TelemetryService.DefaultSession.PostOperation(_namespace + actualName, result);
        }

        public static void TrackException(string name, Exception exception)
        {
            if (string.IsNullOrWhiteSpace(name) || exception == null)
                return;

            string actualName = name.Replace(" ", "_");
            TelemetryService.DefaultSession.PostFault(_namespace + actualName, exception.Message, exception);
        }
    }
}
