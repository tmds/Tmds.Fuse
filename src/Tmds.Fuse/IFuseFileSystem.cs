using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Tmds.Fuse
{
    // This matches the layout of 'struct stat' on linux-x64
    public unsafe struct Stat
    {
        public ulong st_dev { get; set; }
        public ulong st_ino { get; set; }
        public ulong st_nlink { get; set; }
        public uint st_mode { get; set; }
        public uint st_uid { get; set; }
        public uint st_gid { get; set; }
        private int __pad0;
        public ulong st_rdev { get; set; }
        public long st_size { get; set; }
        public long st_blksize { get; set; }
        public TimeSpec st_atim { get; set; }
        public TimeSpec st_mtim { get; set; }
        public TimeSpec st_ctim { get; set; }
        private fixed long __glib_reserved[2];
    }

    public unsafe struct StatVFS
    {
        public ulong b_size { get; set; }
        public ulong f_frsize { get; set; }
        public ulong f_blocks { get; set; }
        public ulong f_bree { get; set; }
        public ulong f_bavail { get; set; }
        public ulong f_files { get; set; }
        public ulong f_ffree { get; set; }
        public ulong f_favail { get; set; }
        public ulong f_fsid { get; set; }
        public ulong f_flag { get; set; }
        public ulong f_namemax { get; set; }
        private fixed int __f_spare[5];
    }

    public ref struct FuseFileInfoRef
    {
        Span<FuseFileInfo> _value;

        public FuseFileInfoRef(Span<FuseFileInfo> fi)
        {
            _value = fi;
        }

        public bool IsNull => _value.IsEmpty;

        public ref FuseFileInfo Value => ref MemoryMarshal.GetReference(_value);
    }

    // This is named FuseFileInfo so it doesn't clash with System.IO.FileInfo
    public struct FuseFileInfo
    {
        public int flags { get; set; }
        private int _bitfields { get; set; }
        private int _padding0;
        private int _padding2;
        public ulong fh { get; set; }
        public ulong lock_owner { get; set; }
        public uint poll_events { get; set; }

        public bool writepage
        {
            get => (_bitfields & WRITEPAGE) != 0;
            set => _bitfields |= WRITEPAGE;
        }

        public bool direct_io
        {
            get => (_bitfields & DIRECTIO) != 0;
            set => _bitfields |= DIRECTIO;
        }

        public bool keep_cache
        {
            get => (_bitfields & KEEPCACHE) != 0;
            set => _bitfields |= KEEPCACHE;
        }

        private const int WRITEPAGE = 1;
        private const int DIRECTIO = 2;
        private const int KEEPCACHE = 3;
    }

    // This matches the layout of 'struct timespec' on linux-x64
    public unsafe struct TimeSpec
    {
        public long tv_sec { get; set; }
        public long tv_nsec { get; set; }

        public bool IsNow => tv_sec == UTIME_NOW;
        public bool IsOmit => tv_sec == UTIME_OMIT;

        public DateTime ToDateTime()
        {
            if (IsNow || IsOmit)
            {
                throw new InvalidOperationException("Cannot convert meta value to DateTime");
            }
            return new DateTime(UnixEpochTicks + TimeSpan.TicksPerSecond * tv_sec + tv_nsec / 100, DateTimeKind.Utc);
        }

        public static implicit operator TimeSpec(DateTime dateTime)
        {
            dateTime = dateTime.ToUniversalTime();
            long ticks = dateTime.Ticks - UnixEpochTicks;
            long sec = ticks / TimeSpan.TicksPerSecond;
            ticks -= TimeSpan.TicksPerSecond * sec;
            long nsec = ticks * 100;
            return new TimeSpec { tv_sec = sec, tv_nsec = nsec };
        }

        private const long UnixEpochTicks = 621355968000000000;
        public const int UTIME_OMIT = 1073741822;
        public const int UTIME_NOW = 1073741823;
    }

    public unsafe ref struct DirectoryContent
    {
        private const int FUSE_NAME_MAX = 1024;
        private readonly void* _buffer;
        private readonly fuse_fill_dir_Delegate _fillDelegate;

        internal DirectoryContent(void* buffer, fuse_fill_dir_Delegate fillDelegate)
        {
            _buffer = buffer;
            _fillDelegate = fillDelegate;
        }

        public void AddEntry(string name) // TODO: extend API
        {
            if (name.Length > 1025) // 1025 = Encoding.UTF8.GetMaxCharCount(FUSE_NAME_MAX)
            {
                ThrowNameTooLongException();
            }
            int maxByteLength = Encoding.UTF8.GetMaxByteCount(name.Length);
            Span<byte> buffer = stackalloc byte[maxByteLength + 1]; // TODO: avoid stackalloc zero-ing
            fixed (byte* bytesPtr = buffer)
            fixed (char* charsPtr = name)
            {
                int length = Encoding.UTF8.GetBytes(charsPtr, name.Length, bytesPtr, maxByteLength);
                if (length > FUSE_NAME_MAX)
                {
                    ThrowNameTooLongException();
                }
                buffer[length] = 0;
                _fillDelegate(_buffer, bytesPtr, null, 0, 0);
            }
        }

        public void AddEntry(ReadOnlySpan<byte> name)
        {
            if (name[name.Length - 1] == 0)
            {
                if (name.Length > (FUSE_NAME_MAX + 1))
                {
                    ThrowNameTooLongException();
                }
                fixed (byte* bytesPtr = name)
                {
                    _fillDelegate(_buffer, bytesPtr, null, 0, 0);
                }
            }
            else
            {
                if (name.Length > FUSE_NAME_MAX)
                {
                    ThrowNameTooLongException();
                }
                Span<byte> buffer = stackalloc byte[name.Length + 1];
                name.CopyTo(buffer);
                buffer[name.Length] = 0;
                fixed (byte* bytesPtr = buffer)
                {
                    _fillDelegate(_buffer, bytesPtr, null, 0, 0);
                }
            }
        }

        private void ThrowNameTooLongException()
        {
            throw new ArgumentException($"The name is too long. Names may be up to {FUSE_NAME_MAX} bytes.", "name");
        }
    }

    public interface IFuseFileSystem : IDisposable
    {
        int GetAttr(ReadOnlySpan<byte> path, ref Stat stat, FuseFileInfoRef fi);
        int Chown(ReadOnlySpan<byte> path, uint uid, uint gid, FuseFileInfoRef fi);
        int Open(ReadOnlySpan<byte> path, FuseFileInfoRef fi);
        void Release(ReadOnlySpan<byte> path, FuseFileInfoRef fi);
        int Rename(ReadOnlySpan<byte> path, ReadOnlySpan<byte> newPath, int flags); // TODO: verify argument names
        int Read(ReadOnlySpan<byte> path, ulong offset, Span<byte> buffer, FuseFileInfoRef fi);
        int ReadDir(ReadOnlySpan<byte> path, ulong offset, ReadDirFlags flags, DirectoryContent content, FuseFileInfoRef fi);
        int SymLink(ReadOnlySpan<byte> path, ReadOnlySpan<byte> target); // TODO: verify argument names
        int RmDir(ReadOnlySpan<byte> path);
        int Unlink(ReadOnlySpan<byte> path);
        int MkDir(ReadOnlySpan<byte> path, uint mode);
        int Create(ReadOnlySpan<byte> path, uint mode, FuseFileInfoRef fi);
        int ReadLink(ReadOnlySpan<byte> path, Span<byte> buffer);
        int Truncate(ReadOnlySpan<byte> path, ulong length, FuseFileInfoRef fi);
        int Write(ReadOnlySpan<byte> path, ulong offset, ReadOnlySpan<byte> buffer, FuseFileInfoRef fi);
        int StatFS(ReadOnlySpan<byte> path, ref StatVFS statfs);
        int ChMod(ReadOnlySpan<byte> path, uint mode, FuseFileInfoRef fi);
        int Link(ReadOnlySpan<byte> fromPath, ReadOnlySpan<byte> toPath);
        int UpdateTimestamps(ReadOnlySpan<byte> path, ref TimeSpec atime, ref TimeSpec mtime, FuseFileInfoRef fi);
        int Flush(ReadOnlySpan<byte> path, FuseFileInfoRef fi);
        int FSync(ReadOnlySpan<byte> path, FuseFileInfoRef fi);
        int SetXAttr(ReadOnlySpan<byte> path, ReadOnlySpan<byte> name, ReadOnlySpan<byte> data, int flags);
        int GetXAttr(ReadOnlySpan<byte> path, ReadOnlySpan<byte> name, Span<byte> data);
        int ListXAttr(ReadOnlySpan<byte> path, Span<byte> list);
        int RemoveXAttr(ReadOnlySpan<byte> path, ReadOnlySpan<byte> name);
        int OpenDir(ReadOnlySpan<byte> path, FuseFileInfoRef fi);
        int ReleaseDir(ReadOnlySpan<byte> path, FuseFileInfoRef fi);
        int FSyncDir(ReadOnlySpan<byte> readOnlySpan, bool onlyData, FuseFileInfoRef fuseFileInfoRef);
        int Access(ReadOnlySpan<byte> path, uint mode);
        int FAllocate(ReadOnlySpan<byte> path, int mode, ulong offset, long length, FuseFileInfoRef fuseFileInfoRef);
    }

    public class FuseFileSystemBase : IFuseFileSystem
    {
        public virtual int Access(ReadOnlySpan<byte> path, uint mode)
            => FuseConstants.ENOSYS;

        public virtual int ChMod(ReadOnlySpan<byte> path, uint mode, FuseFileInfoRef fi)
            => FuseConstants.ENOSYS;

        public virtual int Chown(ReadOnlySpan<byte> path, uint uid, uint gid, FuseFileInfoRef fi)
            => FuseConstants.ENOSYS;

        public virtual int Create(ReadOnlySpan<byte> path, uint mode, FuseFileInfoRef fi)
            => FuseConstants.ENOSYS;

        public virtual void Dispose()
        { }

        public virtual int FAllocate(ReadOnlySpan<byte> path, int mode, ulong offset, long length, FuseFileInfoRef fuseFileInfoRef)
            => FuseConstants.ENOSYS;

        public virtual int Flush(ReadOnlySpan<byte> path, FuseFileInfoRef fi)
            => FuseConstants.ENOSYS;

        public virtual int FSync(ReadOnlySpan<byte> path, FuseFileInfoRef fi)
            => FuseConstants.ENOSYS;

        public virtual int FSyncDir(ReadOnlySpan<byte> readOnlySpan, bool onlyData, FuseFileInfoRef fuseFileInfoRef)
            => FuseConstants.ENOSYS;

        public virtual int GetAttr(ReadOnlySpan<byte> path, ref Stat stat, FuseFileInfoRef fi)
            => FuseConstants.ENOSYS;

        public virtual int GetXAttr(ReadOnlySpan<byte> path, ReadOnlySpan<byte> name, Span<byte> data)
            => FuseConstants.ENOSYS;

        public virtual int Link(ReadOnlySpan<byte> fromPath, ReadOnlySpan<byte> toPath)
            => FuseConstants.ENOSYS;

        public virtual int ListXAttr(ReadOnlySpan<byte> path, Span<byte> list)
            => FuseConstants.ENOSYS;

        public virtual int MkDir(ReadOnlySpan<byte> path, uint mode)
            => FuseConstants.ENOSYS;

        public virtual int Open(ReadOnlySpan<byte> path, FuseFileInfoRef fi)
            => FuseConstants.ENOSYS;

        public virtual int OpenDir(ReadOnlySpan<byte> path, FuseFileInfoRef fi)
            => FuseConstants.ENOSYS;

        public virtual int Read(ReadOnlySpan<byte> path, ulong offset, Span<byte> buffer, FuseFileInfoRef fi)
            => FuseConstants.ENOSYS;

        public virtual int ReadDir(ReadOnlySpan<byte> path, ulong offset, ReadDirFlags flags, DirectoryContent content, FuseFileInfoRef fi)
            => FuseConstants.ENOSYS;

        public virtual int ReadLink(ReadOnlySpan<byte> path, Span<byte> buffer)
            => FuseConstants.ENOSYS;

        public virtual void Release(ReadOnlySpan<byte> path, FuseFileInfoRef fi)
        { }

        public virtual int ReleaseDir(ReadOnlySpan<byte> path, FuseFileInfoRef fi)
            => FuseConstants.ENOSYS;

        public virtual int RemoveXAttr(ReadOnlySpan<byte> path, ReadOnlySpan<byte> name)
            => FuseConstants.ENOSYS;
        public virtual int Rename(ReadOnlySpan<byte> path, ReadOnlySpan<byte> newPath, int flags)
            => FuseConstants.ENOSYS;

        public virtual int RmDir(ReadOnlySpan<byte> path)
            => FuseConstants.ENOSYS;

        public virtual int SetXAttr(ReadOnlySpan<byte> path, ReadOnlySpan<byte> name, ReadOnlySpan<byte> data, int flags)
            => FuseConstants.ENOSYS;

        public virtual int StatFS(ReadOnlySpan<byte> path, ref StatVFS statfs)
            => FuseConstants.ENOSYS;

        public virtual int SymLink(ReadOnlySpan<byte> path, ReadOnlySpan<byte> target)
            => FuseConstants.ENOSYS;

        public virtual int Truncate(ReadOnlySpan<byte> path, ulong length, FuseFileInfoRef fi)
            => FuseConstants.ENOSYS;

        public virtual int Unlink(ReadOnlySpan<byte> path)
            => FuseConstants.ENOSYS;

        public virtual int UpdateTimestamps(ReadOnlySpan<byte> path, ref TimeSpec atime, ref TimeSpec mtime, FuseFileInfoRef fi)
            => FuseConstants.ENOSYS;

        public virtual int Write(ReadOnlySpan<byte> path, ulong off, ReadOnlySpan<byte> span, FuseFileInfoRef fi)
            => FuseConstants.ENOSYS;
    }

    public enum ReadDirFlags { }
}