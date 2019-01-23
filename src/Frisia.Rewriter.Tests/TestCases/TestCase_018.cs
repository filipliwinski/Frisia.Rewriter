class TestCase_018
{
    public static int NWD(int a, int b)
    {
        if (a == 0)
        {
            if (b == 0)
            {
                throw new System.ArgumentException();
            }
            return b;
        }
        if (b == 0)
        {
            return a;
        }
        if (a > b)
        {
            return NWD(a - b, b);
        }
        if (b > a)
        {
            return NWD(a, b - a);
        }
        return a;
    }
}