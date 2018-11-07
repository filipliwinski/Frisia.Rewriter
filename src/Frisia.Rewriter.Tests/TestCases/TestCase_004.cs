using System;

class TestCase_004
{
    public static int Suma2a(int a, int n)
    {
        if (n <= 0)
        {
            throw new ArgumentException("n <= 0");
        }
        else
        {
            Console.WriteLine("temp");
        }
        var s = 0;
        for (int i = a + 1; i < n; i++)
        {
            if (i % 5 == 0 && i % 7 != 0)
            {
                s += i;
            }
        }
        return s;
    }
}
