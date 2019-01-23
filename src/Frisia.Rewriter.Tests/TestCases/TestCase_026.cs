class TestCase_026
{
    public static int Silnia(int n)
    {
        if (n < 0)
        {
            throw new System.ArgumentException();
        }
        var result = 1;
        for (int i = 1; i <= n; i++)
        {
            result = result * i;
        }
        return result;
    }
}