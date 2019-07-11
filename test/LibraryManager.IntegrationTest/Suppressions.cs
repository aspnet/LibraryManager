using Microsoft.VisualStudio.TestTools.UnitTesting;

// Suppress integration tests from running under LUT.
[assembly: TestCategory("SkipWhenLiveUnitTesting")]
