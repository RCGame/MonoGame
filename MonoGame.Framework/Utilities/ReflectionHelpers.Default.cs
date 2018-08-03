using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Utilities
{
#if WebGL
    internal class Marshal
    {
        internal static int SizeOf<T>()
        {
            return 1;
        }

        internal static int SizeOf(Type type)
        {
            return 1;
        }
    }
#endif

    internal static partial class ReflectionHelpers
    {
        /// <summary>
        /// Generics handler for Marshal.SizeOf
        /// </summary>
        internal static class SizeOf<T>
        {
            static int _sizeOf;

            static SizeOf()
            {
                _sizeOf = Marshal.SizeOf<T>();
            }

            static public int Get()
            {
                return _sizeOf;
            }
        }

        /// <summary>
        /// Fallback handler for Marshal.SizeOf(type)
        /// </summary>
        internal static int ManagedSizeOf(Type type)
        {
            return Marshal.SizeOf(type);
        }
    }
}
