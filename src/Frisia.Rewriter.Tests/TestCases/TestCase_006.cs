using System;
using System.Collections.Generic;
using System.Text;

namespace Frisia.CodeAnalyzer.Tests.TestCases
{
    class TestCase_006
    {
        public static void Method(int a, int b)
        {
            for (int i = a + 1; i < b; i++)
            {
                if (i > 2)
                {
                    //if (i = 4)
                    //{
                    Console.WriteLine($"{i}");
                    //}
                }
            }
        }
    }
}
