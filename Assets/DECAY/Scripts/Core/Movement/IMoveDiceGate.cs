namespace Decay
{
    /// <summary>
    /// One narrow, read-only permission check in the MoveDiceRequest path.
    /// Return None to permit evaluation to continue, or a specific denial reason to stop the request.
    /// </summary>
    public interface IMoveDiceGate
    {
        MoveDiceDenialReason Evaluate(MoveDiceGateContext context);
    }
}
