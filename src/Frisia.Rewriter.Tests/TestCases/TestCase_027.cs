class TestCase_027
{
    public static bool Czy_doskonala(int a)
    {
        if (a < 1)
        {
            throw new System.ArgumentException();
        }

        var suma = 0;
        for (int i = 1; i <= a / 2; i++)
        {
            if (a % i == 0)
            {
                suma++;
            }
        }

        if (a == suma)
        {
            return true;
        }
        return false;
    }
}