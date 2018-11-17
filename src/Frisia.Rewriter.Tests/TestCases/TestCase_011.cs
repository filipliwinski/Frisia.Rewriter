using System;

namespace Frisia.CodeReduction
{
    class TestCase_011
    {
        public static int Method(int a, int b)
        {
            while (a < 10)
            {
                if (b > a)
                {
                    return b;
                }
                b = b + a;
                a++;
            }
            return b;
        }
    }
}