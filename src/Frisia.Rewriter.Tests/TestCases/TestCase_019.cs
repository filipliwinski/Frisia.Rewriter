class TestCase_019
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
        while (b != 0)
        {
            var b_old = b;
            b = a % b;
            a = b_old;
        }

        return a;
    }
}