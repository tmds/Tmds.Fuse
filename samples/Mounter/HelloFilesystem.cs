using System;
using System.Text;
using Tmds.Fuse;
using static Tmds.Fuse.FuseConstants;

namespace Mounter
{
    class HelloFileSystem : FuseFileSystemBase
    {
        private static readonly byte[] _helloFilePath = Encoding.UTF8.GetBytes("/hello");
        private static readonly byte[] _helloFileContent = Encoding.UTF8.GetBytes("hello world!");

        public override int GetAttr(ReadOnlySpan<byte> path, Stat stat, FuseFileInfo fi)
        {
            if (path.SequenceEqual(RootPath))
            {
                stat.Mode = S_IFDIR | 0b111_101_101; // rwxr-xr-x
                stat.NLink = 2; // 2 + nr of subdirectories
                return 0;
            }
            else if (path.SequenceEqual(_helloFilePath))
            {
                stat.Mode = S_IFREG | 0b100_100_100; // r--r--r--
                stat.NLink = 1;
                stat.Size = _helloFileContent.Length;
                return 0;
            }
            else
            {
                return ENOENT;
            }
        }

        public override int Open(ReadOnlySpan<byte> path, FuseFileInfo fi)
        {
            if (!path.SequenceEqual(_helloFilePath))
            {
                return ENOENT;
            }

            if ((fi.Flags & O_ACCMODE) != O_RDONLY)
            {
                return EACCES;
            }

            return 0;
        }

        public override int Read(ReadOnlySpan<byte> path, ulong offset, Span<byte> buffer, FuseFileInfo fi)
        {
            if (offset > (ulong)_helloFileContent.Length)
            {
                return 0;
            }
            int intOffset = (int)offset;
            int length = (int)Math.Min(_helloFileContent.Length - intOffset, buffer.Length);
            _helloFileContent.AsSpan().Slice(intOffset, length).CopyTo(buffer);
            return length;
        }

        public override int ReadDir(ReadOnlySpan<byte> path, ulong offset, ReadDirFlags flags, DirectoryContent content, FuseFileInfo fi)
        {
            if (!path.SequenceEqual(RootPath))
            {
                return ENOENT;
            }
            content.AddEntry(".");
            content.AddEntry("..");
            content.AddEntry("hello");
            return 0;
        }
    }
}