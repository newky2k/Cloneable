using System;

namespace LoDaTek.Cloneable.Core
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public sealed class CloneableAttribute : Attribute
    {
        public CloneableAttribute()
        {
        }

        public bool ExplicitDeclaration { get; set; }
    }
}
