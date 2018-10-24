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

        private void ThrowNameTooLongException()
        {
            throw new ArgumentException($"The name is too long. Names may be up to {FUSE_NAME_MAX} bytes.", "name");
        }
    }

    public interface IFuseFileSystem
    {
        int GetAttr(ReadOnlySpan<byte> path, Stat stat, FileInfo fi);
        int Open(ReadOnlySpan<byte> path, FileInfo fi);
        int Read(ReadOnlySpan<byte> path, ulong offset, Span<byte> buffer, FileInfo fi);
        int ReadDir(ReadOnlySpan<byte> path, ulong offset, ReadDirFlags flags, DirectoryContent content, FileInfo fi);
    }

    public enum ReadDirFlags { }
}