namespace ATMR.Game;

public class DeterministicRng
{
    public uint State;

    public DeterministicRng(uint seed)
    {
        State = seed == 0 ? 1u : seed;
    }

    public uint NextUInt()
    {
        uint x = State;

        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;

        State = x;
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
