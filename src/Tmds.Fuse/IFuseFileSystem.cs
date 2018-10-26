using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Tmds.Fuse
{
    public unsafe ref struct Stat
    {
        private readonly Span<byte> _stat;

        internal Stat(stat* stat)
        {
            _stat = new Span<byte>(stat, StructStatInfo.SizeOf);
        }

        public int Mode
        {
            set => MemoryMarshal.Write<int>(_stat.Slice(StructStatInfo.OffsetOfStMode), ref value);
        }

        public int NLink
        {
            set => MemoryMarshal.Write<int>(_stat.Slice(StructStatInfo.OffsetOfNLink), ref value);
        }

        public ulong SizeLong
        {
            set => MemoryMarshal.Write<ulong>(_stat.Slice(StructStatInfo.OffsetOfStSize), ref value);
        }

        public int Size
        {
            set => SizeLong = (ulong)value;
        }

        internal void Clear() => _stat.Fill(0);
    }

    public unsafe ref struct FileInfo
    {
        private readonly fuse_file_info* _fi;

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

    public interface IFuseFileSystem
    {
        int GetAttr(ReadOnlySpan<byte> path, Stat stat, FileInfo fi);
        int Open(ReadOnlySpan<byte> path, FileInfo fi);
        int Release(ReadOnlySpan<byte> path, FileInfo fi);
        int Read(ReadOnlySpan<byte> path, ulong offset, Span<byte> buffer, FileInfo fi);
        int ReadDir(ReadOnlySpan<byte> path, ulong offset, ReadDirFlags flags, DirectoryContent content, FileInfo fi);
        int RmDir(ReadOnlySpan<byte> path);
        int Unlink(ReadOnlySpan<byte> path);
        int MkDir(ReadOnlySpan<byte> path, int mode);
        int Create(ReadOnlySpan<byte> path, int mode, FileInfo fi);
        int Truncate(ReadOnlySpan<byte> path, ulong length, FileInfo fi);
        int Write(ReadOnlySpan<byte> path, ulong offset, ReadOnlySpan<byte> buffer, FileInfo fi);
    }

    public class FuseFileSystemBase : IFuseFileSystem
    {
        public virtual int Create(ReadOnlySpan<byte> path, int mode, FileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual int GetAttr(ReadOnlySpan<byte> path, Stat stat, FileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual int MkDir(ReadOnlySpan<byte> path, int mode)
            => FuseConstants.ENOSYS;

        public virtual int Open(ReadOnlySpan<byte> path, FileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual int Read(ReadOnlySpan<byte> path, ulong offset, Span<byte> buffer, FileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual int ReadDir(ReadOnlySpan<byte> path, ulong offset, ReadDirFlags flags, DirectoryContent content, FileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual int Release(ReadOnlySpan<byte> path, FileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual int RmDir(ReadOnlySpan<byte> path)
            => FuseConstants.ENOSYS;

        public virtual int Truncate(ReadOnlySpan<byte> path, ulong length, FileInfo fileInfo)
            => FuseConstants.ENOSYS;

        public virtual int Unlink(ReadOnlySpan<byte> path)
            => FuseConstants.ENOSYS;

        public virtual int Write(ReadOnlySpan<byte> path, ulong off, ReadOnlySpan<byte> span, FileInfo fileInfo)
            => FuseConstants.ENOSYS;
    }

    public enum ReadDirFlags { }
}