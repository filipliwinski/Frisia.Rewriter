using System;

namespace Frisia.CodeReduction
{
    class TestCase_007
    {
        public static void Method(int a, int b)
        {
            if (a > 0 && b > 0)
            {
                Console.WriteLine("a > 0 && b > 0");

                if (a > 1 && b > 1)
                {
                    Console.WriteLine("a > 1 && b > 1");

                    if (a > 2 && b > 2)
                    {
                        Console.WriteLine("a > 2 && b > 2");
                    }
                    else
                    {
                        Console.WriteLine("!(a > 2 && b > 2)");
                    }
                }
                else
                {
                    Console.WriteLine("!(a > 1 && b > 1)");

                    if (a > 2 && b > 2)
                    {
                        Console.WriteLine("a > 2 && b > 2");
                    }
                    else
                    {
                        Console.WriteLine("!(a > 2 && b > 2)");
                    }
                }
            }
            else
            {
                Console.WriteLine("!(a > 0 && b > 0)");

                if (a < 1 && b < 1)
                {
                    Console.WriteLine("a < 1 && b < 1");

                    if (a < 2 && b < 2)
                    {
                        Console.WriteLine("a < 2 && b < 2");
                    }
                    else
                    {
                        Console.WriteLine("!(a < 2 && b < 2)");
                    }
                }
                else
                {
                    Console.WriteLine("!(a < 1 && b < 1)");

                    if (a < 2 && b < 2)
                    {
                        Console.WriteLine("a < 2 && b < 2");
                    }
                    else
                    {
                        Console.WriteLine("!(a < 2 && b < 2)");
                    }
                }
            }
        }
    }
}