class TestCase_025
{
    public static int Silnia(int n)
    {
        if (n < 0)
        {
            throw new System.ArgumentException();
        }
        if (n > 1)
        {
            return n * Silnia(n - 1);
        }
        return 1;
    }
}