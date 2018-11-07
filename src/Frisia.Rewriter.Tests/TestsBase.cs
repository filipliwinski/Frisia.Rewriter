using System.Collections.Generic;

namespace Frisia.Rewriter.Tests
{
    public abstract class TestsBase
    {
        private Dictionary<string, string> testCases;

        protected Dictionary<string, string> TestCases
        {
            get { return testCases ?? (testCases = TestCasesProvider.GetAllCases()); }
        }

        protected uint LoopIterations { get { return 2; } }

        // Return empty string to stop debugging
        protected string TestToDebug { get { return ""; } }
    }
}
