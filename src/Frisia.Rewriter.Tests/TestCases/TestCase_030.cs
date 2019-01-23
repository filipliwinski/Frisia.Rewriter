class TestCase_030
{
    public static bool Czy_doskonala(int liczba)
    {
        if (liczba < 1)
        {
            throw new System.ArgumentException();
        }

        int dzielnik = 1;
        var suma = 0;
        while (dzielnik <= liczba / 2)
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