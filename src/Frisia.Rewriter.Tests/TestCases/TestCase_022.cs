class TestCase_022
{
    public static int Silnia(int n)
    {
        var result = 1;
        for (int i = 1; i <= n; i++)
        {
            result = result * i;
        }
        return result;
    }
}