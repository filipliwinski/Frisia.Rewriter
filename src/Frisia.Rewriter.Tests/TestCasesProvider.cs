using System.Collections.Generic;
using System.IO;

namespace Frisia.Rewriter.Tests
{
    public static class TestCasesProvider
    {
        public static Dictionary<string, string> GetAllCases()
        {
            var testCases = new Dictionary<string, string>();

            var testCasesPath = Path.Combine(Directory.GetCurrentDirectory(), "TestCases");

            var files = Directory.EnumerateFiles(testCasesPath);

            foreach (var file in files)
            {
                var fileName = file.Split("\\")[file.Split("\\").Length - 1];
                testCases.Add(fileName, File.ReadAllText(file));
            }

            return testCases;
        }

        public static string GetRewrittenCase(string caseName)
        {
            var testCasesPath = Path.Combine(Directory.GetCurrentDirectory(), "TestCasesRewritten");
            var file = Path.Combine(testCasesPath, caseName);

            if (File.Exists(file))
            {
                return File.ReadAllText(file);
            }

            return "";
        }
    }
}
