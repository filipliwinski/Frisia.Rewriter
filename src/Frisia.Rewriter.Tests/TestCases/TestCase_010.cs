using System;

namespace Frisia.CodeReduction
{
    class TestCase_010
    {
        public static void Method(int a, int b)
        {
            if (a + 1 < b)
            {
                Console.WriteLine("a + 1 < b");

                if (b == 1)
                {
                    Console.WriteLine("b == 1");
                }
                if (b == 2)
                    Console.WriteLine("b == 2");
            }

            Console.WriteLine("end");
        }
    }
}