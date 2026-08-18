namespace Decay
{
    /// <summary>
    /// Single injectable randomness boundary for gameplay rules. Implementations must return
    /// values in [minimumInclusive, maximumExclusive).
    /// </summary>
    public interface IRandomSource
    {
        int NextInt(int minimumInclusive, int maximumExclusive);
    }
}
