class TestCase_016
{
    public static int NWD(int a, int b)
    {
        if (a == 0 && b == 0)
        {
            throw new System.ArgumentException();
        }
        if (a == 0)
        {
            return b;
        }
        if (b == 0)
        {
            return a;
        }
        while (a != b)
        {
            if (a > b)
            {
                a = a - b;
            }
            else
            {
                b = b - a;
            }
            if (a == 100)
            {
                return -1;
            }
        }
        return a;
    }
}