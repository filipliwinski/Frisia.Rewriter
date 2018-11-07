using System;

namespace Frisia.CodeReduction
{
    class TestCase_009
    {
        public int Method(int a, int n)
        {
            if (n <= 0)
            {
                throw new ArgumentException("n <= 0");
            }
            var s = 0;
            if ((a + 1) + 0 < n)
            {
                if ((a + 1) % 5 == 0)
                {
                    if ((a + 1) % 7 != 0)
                    {
                        s += a + 1;
                    }
                }
                if ((a + 1) + 1 < n)
                {
                    if ((a + 1) % 5 == 0)
                    {
                        if ((a + 1) % 7 != 0)
                        {
                            s += (a + 1);
                        }
                    }
                }
            }
            return s;
        }
    }
}