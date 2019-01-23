class TestCase_029
{
    public static bool Czy_doskonala(int liczba)
    {
        int dzielnik = 1;
        var suma = 0;
        while (dzielnik != liczba / 2)
        {
            if (liczba % dzielnik == 0)
            {
                suma = suma + dzielnik;
            }
            dzielnik++;
        }

        if (liczba == suma)
        {
            return true;
        }
        return false;
    }
}