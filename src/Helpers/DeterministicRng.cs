namespace ATMR.Game;

public class DeterministicRng
{
    private uint state;

    public DeterministicRng(uint seed)
    {
        state = seed;
    }

    public uint NextUInt()
    {
        uint x = state;

        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;

        state = x;
        return x;
    }

    public int Range(int min, int max)
    {
        return (int)(NextUInt() % (uint)(max - min)) + min;
    }

    public float NextFloat()
    {
        return NextUInt() / (float)uint.MaxValue;
    }
}
