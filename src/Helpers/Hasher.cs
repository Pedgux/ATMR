namespace ATMR.Game;

public static class Hasher
{
    public static uint Hash(uint x)
    {
        x ^= x >> 16;
        x *= 0x7feb352d;
        x ^= x >> 15;
        x *= 0x846ca68b;
        x ^= x >> 16;
        return x;
    }

    public static uint StringHash(string code)
    {
        uint hash = 2166136261;

        foreach (char c in code)
        {
            hash ^= c;
            hash *= 16777619;
        }

        return hash;
    }
}
