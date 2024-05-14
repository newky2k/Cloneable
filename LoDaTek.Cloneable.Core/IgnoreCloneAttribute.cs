using System;
using System.Collections.Generic;
using System.Text;

namespace LoDaTek.Cloneable.Core
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class IgnoreCloneAttribute : Attribute
    {
        public IgnoreCloneAttribute()
        {

        }
    }
}
