using System;
using System.Collections.Generic;
using System.Text;

namespace LoDaTek.Cloneable.Core
{
    /// <summary>
    /// CloneAttribute. This class cannot be inherited.
    /// Implements the <see cref="Attribute" />
    /// </summary>
    /// <seealso cref="Attribute" />
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class CloneAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloneAttribute"/> class.
        /// </summary>
        public CloneAttribute()
        {

        }

        /// <summary>
        /// Gets or sets a value indicating whether [prevent deep copy].
        /// </summary>
        /// <value><c>true</c> if [prevent deep copy]; otherwise, <c>false</c>.</value>
        public bool PreventDeepCopy { get; set; }
    }
}
