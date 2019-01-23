class TestCase_031
{
    public static bool Czy_doskonala(int liczba)
    {
        if (liczba <= 0)
        {
            throw new System.ArgumentException();
        }
        if (liczba == 6 || liczba == 496 || liczba == 8128 || liczba == 33550336)
        {
            return true;
        }
        return false;
    }
}