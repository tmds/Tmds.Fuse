using System;
using System.Text;
using Tmds.Fuse;
using Tmds.Linux;
using static Tmds.Linux.LibC;

namespace Mounter
{
    class HelloFileSystem : FuseFileSystemBase
    {
        private static readonly string _helloFilePath = "/hello";
        private static readonly byte[] _helloFileContent = Encoding.UTF8.GetBytes("hello world!");

        public override bool SupportsMultiThreading => true;

        public override int GetAttr(ReadOnlySpan<char> path, ref stat stat, FuseFileInfoRef fiRef)
        {
            if (path.SequenceEqual(RootPath))
            {
                stat.st_mode = S_IFDIR | 0b111_101_101; // rwxr-xr-x
                stat.st_nlink = 2; // 2 + nr of subdirectories
                return 0;
            }
            else if (path.SequenceEqual(_helloFilePath))
            {
                stat.st_mode = S_IFREG | 0b100_100_100; // r--r--r--
                stat.st_nlink = 1;
                stat.st_size = _helloFileContent.Length;
                return 0;
            }
            else
            {
                return -ENOENT;
            }
        }

        public override int Open(ReadOnlySpan<char> path, ref FuseFileInfo fi)
        {
            if (!path.SequenceEqual(_helloFilePath))
            {
                return -ENOENT;
            }

            if ((fi.flags & O_ACCMODE) != O_RDONLY)
            {
                return -EACCES;
            }

            return 0;
        }

        public override int Read(ReadOnlySpan<char> path, ulong offset, Span<byte> buffer, ref FuseFileInfo fi)
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

        public override int ReadDir(ReadOnlySpan<char> path, ulong offset, ReadDirFlags flags, DirectoryContent content, ref FuseFileInfo fi)
        {
            if (!path.SequenceEqual(RootPath))
            {
                return -ENOENT;
            }
            content.AddEntry(".");
            content.AddEntry("..");
            content.AddEntry("hello");
            return 0;
        }
    }
}