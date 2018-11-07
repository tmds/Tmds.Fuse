using System;
using System.Runtime.InteropServices;
using System.Text;
using static Tmds.Fuse.PlatformConstants;

namespace Tmds.Fuse
{
    static class MemoryHelper
    {
        public static unsafe void WriteSizeOf(int size, void* memory, int offset, long value)
        {
            if (size == 4)
            {
                Write<int>(memory, offset, (int)value);
            }
            else if (size == 8)
            {
                Write<long>(memory, offset, value);
            }
        }

        public static unsafe long ReadSizeOf(int size, void* memory, int offset)
        {
            if (size == 4)
            {
                return Read<int>(memory, offset);
            }
            else if (size == 8)
            {
                return Read<long>(memory, offset);
            }
            else
            {
                return -1;
            }
        }

        public unsafe static void Write<T>(void* memory, int offset, T value) where T : unmanaged
            => *(T*)((byte*)memory + offset) = value;

        public unsafe static T Read<T>(void* memory, int offset) where T : unmanaged
            => *(T*)((byte*)memory + offset);
    }

    public unsafe ref struct Stat
    {
        private readonly stat* _stat;

        internal Stat(stat* stat)
            => _stat = stat;

        public int Mode
        {
            set => MemoryHelper.Write<int>(_stat, StatOffsetOfStMode, value);
        }

        public int NLink
        {
            set => MemoryHelper.Write<IntPtr>(_stat, StatOffsetOfNLink, new IntPtr(value));
        }

        public ulong SizeLong
        {
            set => MemoryHelper.Write<ulong>(_stat, StatOffsetOfStSize, value);
        }

        public int Size
        {
            set => SizeLong = (ulong)value;
        }

        internal void Clear()
        {
            for (int i = 0; i < StatSizeOf; i++)
            {
                *((byte*)_stat + i) = 0;
            }
        }

        public long ATimeSec
        {
            set => MemoryHelper.WriteSizeOf(TimeTSizeOf, _stat, StatOffsetOfStATime, value);
        }

        public long ATimeNSec
        {
            set => MemoryHelper.Write<IntPtr>(_stat, StatOffsetOfStATimeNsec, new IntPtr(value));
        }

        public DateTime ATime
        {
            set
            {
                long sec, nsec;
                GetTimeValues(value, out sec, out nsec);
                ATimeSec = sec;
                ATimeNSec = nsec;
            }
        }

        public long MTimeSec
        {
            set => MemoryHelper.WriteSizeOf(TimeTSizeOf, _stat, StatOffsetOfStMTime, value);
        }

        public long MTimeNSec
        {
            set => MemoryHelper.Write<IntPtr>(_stat, StatOffsetOfStMTimeNsec, new IntPtr(value));
        }

        public DateTime MTime
        {
            set
            {
                long sec, nsec;
                GetTimeValues(value, out sec, out nsec);
                MTimeSec = sec;
                MTimeNSec = nsec;
            }
        }

        private void GetTimeValues(DateTime value, out long sec, out long nsec)
        {
            value = value.ToUniversalTime();
            long ticks = value.Ticks - UnixEpochTicks;
            sec = ticks / TimeSpan.TicksPerSecond;
            ticks -= TimeSpan.TicksPerSecond * sec;
            nsec = ticks * 100;
        }

        private const long UnixEpochTicks = 621355968000000000;
    }

    public unsafe ref struct FileInfo
    {
        private readonly fuse_file_info* _fi;

        public bool IsNull => _fi == null;

        internal FileInfo(fuse_file_info* fi)
        {
            _fi = fi;
        }

        public int Flags
        {
            get => _fi->flags;
            set => _fi->flags = value;
        }

        public ulong FileDescriptor
        {
            get => _fi->fh;
            set => _fi->fh = value;
        }

        public bool DirectIO
        {
            get => (_fi->bitfields & FileInfoDirectIoFieldMask) != 0;
            set
            {
                if (value)
                {
                    _fi->bitfields |= FileInfoDirectIoFieldMask;
                }
                else
                {
                    _fi->bitfields &= ~FileInfoDirectIoFieldMask;
                }
            }
        }
    }

    public unsafe ref struct TimeSpec
    {
        private readonly timespec* _ts;

        internal TimeSpec(timespec* ts)
            => _ts = ts;

        public long Sec
        {
            get => MemoryHelper.ReadSizeOf(TimeTSizeOf, _ts, TimespecOffsetOfTvSec);
            set => MemoryHelper.WriteSizeOf(TimeTSizeOf, _ts, TimespecOffsetOfTvSec, value);
        }

        public long NSec
        {
            get => MemoryHelper.Read<IntPtr>(_ts, TimespecOffsetOfTvNsec).ToInt64();
            set => MemoryHelper.Write<IntPtr>(_ts, TimespecOffsetOfTvNsec, new IntPtr(value));
        }

        public bool IsNow => NSec == UTIME_NOW;
        public bool IsOmit => NSec == UTIME_OMIT;

        public DateTime ToDateTime()
        {
            if (IsNow || IsOmit)
            {
                throw new InvalidOperationException("Cannot convert meta value to DateTime");
            }
            return new DateTime(UnixEpochTicks + TimeSpan.TicksPerSecond * Sec + NSec / 100, DateTimeKind.Utc);
        }

        private const long UnixEpochTicks = 621355968000000000;
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
        int GetAttr(ReadOnlySpan<byte> path, Stat stat, FileInfo fi);
        int Open(ReadOnlySpan<byte> path, FileInfo fi);
        void Release(ReadOnlySpan<byte> path, FileInfo fi);
        int Read(ReadOnlySpan<byte> path, ulong offset, Span<byte> buffer, FileInfo fi);
        int ReadDir(ReadOnlySpan<byte> path, ulong offset, ReadDirFlags flags, DirectoryContent content, FileInfo fi);
        int RmDir(ReadOnlySpan<byte> path);
        int Unlink(ReadOnlySpan<byte> path);
        int MkDir(ReadOnlySpan<byte> path, int mode);
        int Create(ReadOnlySpan<byte> path, int mode, FileInfo fi);
        int Truncate(ReadOnlySpan<byte> path, ulong length, FileInfo fi);
        int Write(ReadOnlySpan<byte> path, ulong offset, ReadOnlySpan<byte> buffer, FileInfo fi);
        int ChMod(ReadOnlySpan<byte> path, int mode, FileInfo fi);
        int Link(ReadOnlySpan<byte> fromPath, ReadOnlySpan<byte> toPath);
        int UpdateTimestamps(ReadOnlySpan<byte> path, TimeSpec atime, TimeSpec mtime, FileInfo fi);
    }

    public class FuseFileSystemBase : IFuseFileSystem
    {
        public virtual int ChMod(ReadOnlySpan<byte> path, int mode, FileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual int Create(ReadOnlySpan<byte> path, int mode, FileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual void Dispose()
        { }

        public virtual int GetAttr(ReadOnlySpan<byte> path, Stat stat, FileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual int Link(ReadOnlySpan<byte> fromPath, ReadOnlySpan<byte> toPath)
            => FuseConstants.ENOSYS;

        public virtual int MkDir(ReadOnlySpan<byte> path, int mode)
            => FuseConstants.ENOSYS;

        public virtual int Open(ReadOnlySpan<byte> path, FileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual int Read(ReadOnlySpan<byte> path, ulong offset, Span<byte> buffer, FileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual int ReadDir(ReadOnlySpan<byte> path, ulong offset, ReadDirFlags flags, DirectoryContent content, FileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual void Release(ReadOnlySpan<byte> path, FileInfo fi)
        { }

        public virtual int RmDir(ReadOnlySpan<byte> path)
            => FuseConstants.ENOSYS;

        public virtual int Truncate(ReadOnlySpan<byte> path, ulong length, FileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual int Unlink(ReadOnlySpan<byte> path)
            => FuseConstants.ENOSYS;

        public virtual int UpdateTimestamps(ReadOnlySpan<byte> path, TimeSpec atime, TimeSpec mtime, FileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual int Write(ReadOnlySpan<byte> path, ulong off, ReadOnlySpan<byte> span, FileInfo fi)
            => FuseConstants.ENOSYS;
    }

    public enum ReadDirFlags { }
}