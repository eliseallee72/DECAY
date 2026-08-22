namespace Decay
{
    /// <summary>
    /// Presentation-only pointer feedback contract. Hover/press feedback never approves gameplay interaction;
    /// functional controls continue to submit their normal authoritative requests independently.
    /// </summary>
    internal interface IPointerPresentationTarget
    {
        bool PointerPresentationEnabled { get; }
        void SetPointerHovered(bool isHovered);
        void PlayPointerPressPresentation();
    }
}
