using System;

namespace Workflow.Masstransit
{
    /// <summary> Extension Methods for <see cref="Action{T}"/> </summary>
    public static class XAction
    {

        /// <summary> AKA Append; Extension Method to concatenate Sequences of Actions </summary>
        public static Action<T0> Concat<T0>(this Action<T0> a0, Action<T0> a1) 
            => a => { a0(a); a1(a); };

        /// <summary> AKA Append; Extension Method to concatenate Sequences of Actions </summary>
        public static Action<T0, T1> Concat<T0, T1>(this Action<T0, T1> a0, Action<T0, T1> a1) 
            => (a, b) => { a0(a, b); a1(a, b); };

    }
}