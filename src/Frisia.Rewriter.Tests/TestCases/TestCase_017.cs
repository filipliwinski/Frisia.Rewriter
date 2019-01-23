class TestCase_017
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
        while (a != b)
        {
            if (a > b)
            {
                a = a % b;
            }
            else
            {
                b = b % a;
            }
        }
        return a;
    }
}