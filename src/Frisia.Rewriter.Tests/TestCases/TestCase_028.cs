class TestCase_028
{
    public static bool Czy_doskonala(int a)
    {
        if (a <= 0)
        {
            throw new System.ArgumentException();
        }

        var suma = 0;
        for (int i = 1; i < a / 2; i++)
        {
            if (a % i == 0)
            {
                suma = suma + i;
            }
        }

        if (a == suma)
        {
            return true;
        }
        return false;
    }
}