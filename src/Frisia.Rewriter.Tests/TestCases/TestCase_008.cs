using System;

namespace Frisia.CodeReduction
{
    class TestCase_008
    {
        public int Method(int a, int b)
        {
            if (a <= 0)
            {
                throw new ArgumentException("a <= 0");
            }
            if (b < 0)
            {
                Console.WriteLine("b < 0");
            }
            a = a * b;
            return a;
        }
    }
}