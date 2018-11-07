using System;
using System.Runtime.Serialization;

namespace Frisia.Rewriter.Tests
{
    public class TestCaseFailedException : Exception
    {
        public TestCaseFailedException()
        {
        }

        public TestCaseFailedException(string testCase) : base($"Test failed for {testCase}.")
        {
        }

        public TestCaseFailedException(string testCase, Exception innerException) : base($"Test failed for {testCase}.", innerException)
        {
        }

        protected TestCaseFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
