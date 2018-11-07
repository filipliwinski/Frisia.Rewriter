using System;

namespace Frisia.CodeAnalyzer.Tests
{
    class TestCase_001
    {
        public static void Method(int a, int b, int c)
        {
            if (a > 0 && b > 0 && c > 0)
            {
                Console.WriteLine("a > 0 && b > 0 && c > 0");
            }
            else
                Console.WriteLine("!(a > 0 && b > 0 && c > 0)");
        }
    }
}