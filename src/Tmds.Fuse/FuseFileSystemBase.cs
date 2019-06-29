using System;
using System.Runtime.InteropServices;
using System.Text;
using static Tmds.Linux.LibC;
using Tmds.Linux;

namespace Tmds.Fuse
{
    public class FuseFileSystemBase : IFuseFileSystem
    {
        public static string RootPath => "/";

        public virtual bool SupportsMultiThreading => false;

        public virtual int Access(ReadOnlySpan<char> path, mode_t mode)
            => -ENOSYS;

        public virtual int ChMod(ReadOnlySpan<char> path, mode_t mode, FuseFileInfoRef fiRef)
            => -ENOSYS;

        public virtual int Chown(ReadOnlySpan<char> path, uint uid, uint gid, FuseFileInfoRef fiRef)
            => -ENOSYS;

        public virtual int Create(ReadOnlySpan<char> path, mode_t mode, ref FuseFileInfo fi)
            => -ENOSYS;

        public virtual void Dispose()
        { }

        public virtual int FAllocate(ReadOnlySpan<char> path, int mode, ulong offset, long length, ref FuseFileInfo fi)
            => -ENOSYS;

        public virtual int Flush(ReadOnlySpan<char> path, ref FuseFileInfo fi)
            => -ENOSYS;

        public virtual int FSync(ReadOnlySpan<char> path, ref FuseFileInfo fi)
            => -ENOSYS;

        public virtual int FSyncDir(ReadOnlySpan<byte> readOnlySpan, bool onlyData, ref FuseFileInfo fi)
            => -ENOSYS;

        public virtual int GetAttr(ReadOnlySpan<char> path, ref stat stat, FuseFileInfoRef fiRef)
            => -ENOSYS;

        public virtual int GetXAttr(ReadOnlySpan<char> path, ReadOnlySpan<char> name, Span<byte> data)
            => -ENOSYS;

        public virtual int Link(ReadOnlySpan<char> fromPath, ReadOnlySpan<char> toPath)
            => -ENOSYS;

        public virtual int ListXAttr(ReadOnlySpan<char> path, Span<byte> list)
            => -ENOSYS;

        public virtual int MkDir(ReadOnlySpan<char> path, mode_t mode)
            => -ENOSYS;

        public virtual int Open(ReadOnlySpan<char> path, ref FuseFileInfo fi)
            => -ENOSYS;

        public virtual int OpenDir(ReadOnlySpan<char> path, ref FuseFileInfo fi)
            => 0;

        public virtual int Read(ReadOnlySpan<char> path, ulong offset, Span<byte> buffer, ref FuseFileInfo fi)
            => -ENOSYS;

        public virtual int ReadDir(ReadOnlySpan<char> path, ulong offset, ReadDirFlags flags, DirectoryContent content, ref FuseFileInfo fi)
            => -ENOSYS;

        public virtual int ReadLink(ReadOnlySpan<char> path, Span<byte> buffer)
            => -ENOSYS;

        public virtual void Release(ReadOnlySpan<char> path, ref FuseFileInfo fi)
        { }

        public virtual int ReleaseDir(ReadOnlySpan<char> path, ref FuseFileInfo fi)
            => -ENOSYS;

        public virtual int RemoveXAttr(ReadOnlySpan<char> path, ReadOnlySpan<char> name)
            => -ENOSYS;
        public virtual int Rename(ReadOnlySpan<char> path, ReadOnlySpan<char> newPath, int flags)
            => -ENOSYS;

        public virtual int RmDir(ReadOnlySpan<char> path)
            => -ENOSYS;

        public virtual int SetXAttr(ReadOnlySpan<char> path, ReadOnlySpan<char> name, ReadOnlySpan<byte> data, int flags)
            => -ENOSYS;

        public virtual int StatFS(ReadOnlySpan<char> path, ref statvfs statfs)
            => -ENOSYS;

        public virtual int SymLink(ReadOnlySpan<char> path, ReadOnlySpan<char> target)
            => -ENOSYS;

        public virtual int Truncate(ReadOnlySpan<char> path, ulong length, FuseFileInfoRef fiRef)
            => -ENOSYS;

        public virtual int Unlink(ReadOnlySpan<char> path)
            => -ENOSYS;

        public virtual int UpdateTimestamps(ReadOnlySpan<char> path, ref timespec atime, ref timespec mtime, FuseFileInfoRef fiRef)
            => -ENOSYS;

        public virtual int Write(ReadOnlySpan<char> path, ulong off, ReadOnlySpan<byte> span, ref FuseFileInfo fi)
            => -ENOSYS;
    }
}