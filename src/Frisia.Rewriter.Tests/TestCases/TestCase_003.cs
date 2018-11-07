using System;

namespace Frisia.CodeAnalyzer.Tests.TestCases
{
    class TestCase_003
    {
        public static void Method(int a, int b)
        {
            if (a > 0 || b > 0)
            {
                Console.WriteLine("a > 0 || b > 0");
            }
            else
            {
                Console.WriteLine("!(a > 0 || b > 0)");

                if (b <= 0)
                {
                    Console.WriteLine("b <= 0");
                }
            }
            if (a < 0 || b < 0)
            {
                Console.WriteLine("a < 0 || b < 0");
            }
        }
    }
}