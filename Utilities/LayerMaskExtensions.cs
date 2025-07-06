using System.Collections.Generic;
using UnityEngine;

public static class LayerMaskExtensions
{
    // /// <summary>
    // /// Checks if a layer (as int) is included in the given LayerMask.
    // /// </summary>
    // public static bool Contains(this LayerMask mask, LayerMask layer)
    // {
    //     return ((1 << layer) & mask) != 0;
    // }

    // /// <summary>
    // /// Checks if a layer (as int) is included in the given LayerMask.
    // /// </summary>
    // public static bool Contains(this int hitLayer, LayerMask layerMask)
    // {
    //     return ((1 << hitLayer) & layerMask) != 0;
    // }

    // public static bool Contains(this LayerMask mask, int layer)
    // {
    //     return (mask.value & (1 << layer)) != 0;
    // }

    // // Check if a mask contains another mask (e.g. single-layer mask)
    // public static bool Contains(this LayerMask mask, LayerMask other)
    // {
    //     return (mask.value & other.value) != 0;
    // }

    /// <summary>
    /// Returns true if there is any overlap between the two LayerMasks.
    /// </summary>
    public static bool ContainsAny(this LayerMask mask, LayerMask other)
    {
        return (mask.value & other.value) != 0;
    }

    /// <summary>
    /// Returns a list of layer names included in the LayerMask.
    /// </summary>
    public static List<string> Names(this LayerMask mask)
    {
        var names = new List<string>();
        for (int i = 0; i < 32; i++)
        {
            if (((1 << i) & mask.value) != 0)
            {
                string name = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(name))
                    names.Add(name);
            }
        }
        return names;
    }

    /// <summary>
    /// Returns a comma-separated string of the layer names included in the LayerMask.
    /// </summary>
    public static string PrintNames(this LayerMask mask)
    {
        var names = mask.Names();
        return names.Count > 0 ? string.Join(", ", names) : "(No named layers)";
    }
    
}