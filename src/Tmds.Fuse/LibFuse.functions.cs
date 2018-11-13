using System;
using System.Runtime.InteropServices;

namespace Tmds.Fuse
{
    using size_t = System.UIntPtr;

    static unsafe class LibFuse
    {
        public const string LibraryName = "libfuse3.so.3";
        private static readonly IntPtr s_libFuseHandle;

        public static bool IsAvailable => s_libFuseHandle != IntPtr.Zero;

        public delegate fuse* fuse_new_Delegate(fuse_args* args, fuse_operations* op, size_t op_size, void* private_data);
        public static readonly fuse_new_Delegate fuse_new;

        public delegate int fuse_loop_Delegate(fuse* f);
        public static readonly fuse_loop_Delegate fuse_loop;

        public delegate int fuse_mount_Delegate(fuse* f, string mountpoint);
        public static readonly fuse_mount_Delegate fuse_mount;

        public delegate int fuse_opt_add_arg_Delegate(fuse_args* args, string arg);
        public static readonly fuse_opt_add_arg_Delegate fuse_opt_add_arg;

        static LibFuse()
        {
            s_libFuseHandle = dlopen(LibraryName, 2);
            if (s_libFuseHandle == IntPtr.Zero)
            {
                return;
            }

            fuse_new = CreateDelegate<fuse_new_Delegate>("fuse_new", "FUSE_3.1");
            fuse_loop = CreateDelegate<fuse_loop_Delegate>("fuse_loop");
            fuse_mount = CreateDelegate<fuse_mount_Delegate>("fuse_mount");
            fuse_opt_add_arg = CreateDelegate<fuse_opt_add_arg_Delegate>("fuse_opt_add_arg");
        }

        private static T CreateDelegate<T>(string name, string version = "FUSE_3.0")
        {
            IntPtr functionPtr = dlvsym(s_libFuseHandle, name, version);
            if (functionPtr == IntPtr.Zero)
            {
                throw new FuseException($"Unable to resolve libfuse function {name}:{version}.");
            }
            return  Marshal.GetDelegateForFunctionPointer<T>(functionPtr);
        }

        [DllImport("dl")]
        private static extern IntPtr dlvsym(IntPtr handle, string symbol, string version);

        [DllImport("dl")]
        private static extern IntPtr dlopen(string filename, int flag);

        [DllImport("dl")]
        private static extern IntPtr dlerror();
    }
}