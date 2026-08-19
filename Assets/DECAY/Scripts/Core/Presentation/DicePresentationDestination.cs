using System;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Presentation-only destination resolved from a semantic gameplay location. The authoritative Board/Inventory
    /// owns where a dice belongs; editor-authored anchors own where that location appears in the scene.
    /// </summary>
    internal readonly struct DicePresentationDestination
    {
        internal DicePresentationDestination(Transform anchor, Vector3 localOffset)
        {
            Anchor = anchor != null ? anchor : throw new ArgumentNullException(nameof(anchor));
            LocalOffset = localOffset;
        }

        internal Transform Anchor { get; }
        internal Vector3 LocalOffset { get; }
        internal Vector3 WorldPosition => Anchor.TransformPoint(LocalOffset);
    }
}
