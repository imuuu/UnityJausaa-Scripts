using System;

namespace Game.Utility
{
    /// <summary>
    /// Marks a field, property, or method as used indirectly 
    /// (e.g. by Unity Inspector, Odin Inspector, or reflection),
    /// so code analyzers won't warn it's unused.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, Inherited = false)]
    public sealed class UsedByInspector : Attribute
    {
    }
}
