class TestCase_012
{
    public static int NWD(int a, int b)
    {
        if (a <= 0 || b <= 0)
        {
            throw new System.ArgumentException();
        }
        while (a != b)
        {
            if (a > b)
                a = a - b;
            else
                b = b - a;
        }
        return a;
    }
}