class TestCase_014
{
    public static decimal Power(int n, int w)
    {
        var wAbs = w;
        if (w < 0)
        {
            wAbs = -w;
        }
        int p = n;
        int i = 1;
        while (i < wAbs)
        {
            if (int.MaxValue < (long)p * n)
            {
                throw new System.ArgumentException();
            }
            p = p * n;
            i = i + 1;
        }
        if (w > 0)
            return p;
        return (decimal)1 / p;
    }
}