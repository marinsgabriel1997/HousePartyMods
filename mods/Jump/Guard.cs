using System;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;

namespace Jump
{
    internal static class Guard
    {
        // No IL2CPP, métodos com o mesmo código nativo podem ser fundidos numa
        // única função. Um patch nela atinge todos, de qualquer classe. Por isso
        // cada patch confere a classe real do objeto antes de agir.
        public static bool Is<T>(Il2CppObjectBase obj) where T : Il2CppObjectBase
        {
            if (obj == null || obj.Pointer == IntPtr.Zero)
                return false;

            IntPtr actual = IL2CPP.il2cpp_object_get_class(obj.Pointer);
            return IL2CPP.il2cpp_class_is_assignable_from(Il2CppClassPointerStore<T>.NativeClassPtr, actual);
        }
    }
}
