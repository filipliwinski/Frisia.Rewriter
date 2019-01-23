class TestCase_024
{
    public static int Silnia(int n)
    {
        int wynik = 1;
        if (n < 0)
        {
            throw new System.ArgumentException();
        }
        while (n > 0)
        {
            wynik = wynik * n;
            n--;
        }
        return wynik;
    }
}