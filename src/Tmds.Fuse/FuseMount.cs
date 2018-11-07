using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using static Tmds.Fuse.PlatformConstants;
using static Tmds.Fuse.FuseConstants;

namespace Tmds.Fuse
{
    [System.Serializable]
    public class FuseException : System.Exception
    {
        public FuseException() { }
        public FuseException(string message) : base(message) { }
        public FuseException(string message, System.Exception inner) : base(message, inner) { }
        protected FuseException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    class FuseMount : IDisposable
    {
        private readonly string _mountPoint;
        private readonly IFuseFileSystem _fileSystem;
        private readonly getattr_Delegate _getattr;
        private readonly readdir_Delegate _readdir;
        private readonly open_Delegate _open;
        private readonly read_Delegate _read;
        private readonly release_Delegate _release;
        private readonly write_Delegate _write;
        private readonly unlink_Delegate _unlink;
        private readonly truncate_Delegate _truncate;
        private readonly rmdir_Delegate _rmdir;
        private readonly mkdir_Delegate _mkdir;
        private readonly create_Delegate _create;
        private readonly chmod_Delegate _chmod;
        private readonly link_Delegate _link;
        private readonly utimes_Delegate _utimens;

        private unsafe class ManagedFiller
        {
            public readonly fuse_fill_dir* Filler;
            public readonly fuse_fill_dir_Delegate Delegate;

            public ManagedFiller(fuse_fill_dir* filler, fuse_fill_dir_Delegate fillDelegate)
            {
                Filler = filler;
                Delegate = fillDelegate;
            }
        }
        private ManagedFiller _previousFiller;

        public unsafe FuseMount(string mountPoint, IFuseFileSystem fileSystem)
        {
            _mountPoint = mountPoint;
            _fileSystem = fileSystem;
            _getattr = Getattr;
            _read = Read;
            _open = Open;
            _readdir = Readdir;
            _release = Release;
            _write = Write;
            _unlink = Unlink;
            _truncate = Truncate;
            _rmdir = Rmdir;
            _mkdir = Mkdir;
            _create = Create;
            _chmod = Chmod;
            _link = Link;
            _utimens = Utimens;
        }

        private unsafe int Utimens(path* path, timespec* tv, fuse_file_info* fi)
        {
            try
            {
                TimeSpec atime = new TimeSpec(tv);
                TimeSpec mtime = new TimeSpec((timespec*)((byte*)tv + TimespecSizeOf));
                return _fileSystem.UpdateTimestamps(ToSpan(path), atime, mtime, ToFileInfo(fi));
            }
            catch
            {
                return EIO;
            }
        }

        private unsafe int Link(path* fromPath, path* toPath)
        {
            try
            {
                return _fileSystem.Link(ToSpan(fromPath), ToSpan(toPath));
            }
            catch
            {
                return EIO;
            }
        }

        private unsafe int Chmod(path* path, int mode, fuse_file_info* fi)
        {
            try
            {
                return _fileSystem.ChMod(ToSpan(path), mode, ToFileInfo(fi));
            }
            catch
            {
                return EIO;
            }
        }

        private unsafe int Truncate(path* path, ulong length, fuse_file_info* fi)
        {
            try
            {
                return _fileSystem.Truncate(ToSpan(path), length, ToFileInfo(fi));
            }
            catch
            {
                return EIO;
            }
        }

        private unsafe int Create(path* path, int mode, fuse_file_info* fi)
        {
            try
            {
                return _fileSystem.Create(ToSpan(path), mode, ToFileInfo(fi));
            }
            catch
            {
                return EIO;
            }
        }

        private unsafe int Mkdir(path* path, int mode)
        {
            try
            {
                return _fileSystem.MkDir(ToSpan(path), mode);
            }
            catch
            {
                return EIO;
            }
        }

        private unsafe int Rmdir(path* path)
        {
            try
            {
                return _fileSystem.RmDir(ToSpan(path));
            }
            catch
            {
                return EIO;
            }
        }

        private unsafe int Unlink(path* path)
        {
            try
            {
                return _fileSystem.Unlink(ToSpan(path));}
            catch
            {
                return EIO;
            }
        }

        private unsafe int Write(path* path, void* buffer, UIntPtr size, ulong off, fuse_file_info* fi)
        {
            try
            {
                // TODO: handle size > int.MaxValue
                return _fileSystem.Write(ToSpan(path), off, new ReadOnlySpan<byte>(buffer, (int)size), ToFileInfo(fi));
            }
            catch
            {
                return EIO;
            }
        }

        private unsafe int Getattr(path* path, stat* stat, fuse_file_info* fi)
        {
            try
            {
                Stat s = ToStat(stat);
                s.Clear();
                return _fileSystem.GetAttr(ToSpan(path), s, ToFileInfo(fi));
            }
            catch
            {
                return EIO;
            }
        }

        private unsafe int Readdir(path* path, void* buf, fuse_fill_dir* filler, ulong offset, fuse_file_info* fi, int flags)
        {
            try
            {
                // try to reuse the previous delegate
                fuse_fill_dir_Delegate fillDelegate;
                ManagedFiller previousFiller = _previousFiller;
                if (previousFiller != null && previousFiller.Filler == filler)
                {
                    fillDelegate = previousFiller.Delegate;
                }
                else
                {
                    fillDelegate = Marshal.GetDelegateForFunctionPointer<fuse_fill_dir_Delegate>(new IntPtr(filler));
                    _previousFiller = new ManagedFiller(filler, fillDelegate);
                }

                return _fileSystem.ReadDir(ToSpan(path), offset, (ReadDirFlags)flags, ToDirectoryContent(buf, fillDelegate), ToFileInfo(fi));
            }
            catch
            {
                return EIO;
            }
        }

        private unsafe int Open(path* path, fuse_file_info* fi)
        {
            try
            {
                return _fileSystem.Open(ToSpan(path), ToFileInfo(fi));
            }
            catch
            {
                return EIO;
            }
        }

        private unsafe int Read(path* path, void* buffer, UIntPtr size, ulong off, fuse_file_info* fi)
        {
            try
            {
                // TODO: handle size > int.MaxValue
                return _fileSystem.Read(ToSpan(path), off, new Span<byte>(buffer, (int)size), ToFileInfo(fi));
            }
            catch
            {
                return EIO;
            }
        }

        private unsafe int Release(path* path, fuse_file_info* fi)
        {
            try
            {
                _fileSystem.Release(ToSpan(path), ToFileInfo(fi));
                return 0;
            }
            catch
            {
                return EIO;
            }
        }

        private unsafe FuseFileInfo ToFileInfo(fuse_file_info* fi) => new FuseFileInfo(fi);

        private unsafe Stat ToStat(stat* stat) => new Stat(stat);

        private unsafe ReadOnlySpan<byte> ToSpan(path* path)
        {
            var span = new ReadOnlySpan<byte>(path, int.MaxValue);
            return span.Slice(0, span.IndexOf((byte)0));
        }

        private unsafe DirectoryContent ToDirectoryContent(void* buffer, fuse_fill_dir_Delegate fillDelegate) => new DirectoryContent(buffer, fillDelegate);

        public unsafe void Mount()
        {
            // TODO: delete args
            fuse_args args;
            LibFuse.fuse_opt_add_arg(&args, "");

            fuse_operations ops;
            ops.getattr = Marshal.GetFunctionPointerForDelegate(_getattr);
            ops.readdir = Marshal.GetFunctionPointerForDelegate(_readdir);
            ops.open = Marshal.GetFunctionPointerForDelegate(_open);
            ops.read = Marshal.GetFunctionPointerForDelegate(_read);
            ops.release = Marshal.GetFunctionPointerForDelegate(_release);
            ops.write = Marshal.GetFunctionPointerForDelegate(_write);
            ops.unlink = Marshal.GetFunctionPointerForDelegate(_unlink);
            ops.truncate = Marshal.GetFunctionPointerForDelegate(_truncate);
            ops.rmdir = Marshal.GetFunctionPointerForDelegate(_rmdir);
            ops.mkdir = Marshal.GetFunctionPointerForDelegate(_mkdir);
            ops.create = Marshal.GetFunctionPointerForDelegate(_create);
            ops.chmod = Marshal.GetFunctionPointerForDelegate(_chmod);
            ops.link = Marshal.GetFunctionPointerForDelegate(_link);
            ops.utimens = Marshal.GetFunctionPointerForDelegate(_utimens);

            // TODO: cleanup/unmount
            var fuse = LibFuse.fuse_new(&args, &ops, (UIntPtr)sizeof(fuse_operations), null);
            int rv = LibFuse.fuse_mount(fuse, _mountPoint);
            if (rv != 0)
            {
                ThrowException(nameof(LibFuse.fuse_mount), rv);
            }
            rv = LibFuse.fuse_loop(fuse);
            if (rv != 0)
            {
                ThrowException(nameof(LibFuse.fuse_loop), rv);
            }
        }

        private void ThrowException(string operation, int returnValue)
        {
            throw new FuseException($"Failed to {operation}, the function returned {returnValue}.");
        }

        public void Dispose()
        {
            _fileSystem.Dispose();
        }
    }
}