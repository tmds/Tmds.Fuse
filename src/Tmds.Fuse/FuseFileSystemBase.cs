using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Tmds.Fuse
{
    public class FuseFileSystemBase : IFuseFileSystem
    {
        public virtual bool SupportsMultiThreading => false;

        public virtual int Access(ReadOnlySpan<byte> path, uint mode)
            => FuseConstants.ENOSYS;

        public virtual int ChMod(ReadOnlySpan<byte> path, uint mode, FuseFileInfoRef fiRef)
            => FuseConstants.ENOSYS;

        public virtual int Chown(ReadOnlySpan<byte> path, uint uid, uint gid, FuseFileInfoRef fiRef)
            => FuseConstants.ENOSYS;

        public virtual int Create(ReadOnlySpan<byte> path, uint mode, ref FuseFileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual void Dispose()
        { }

        public virtual int FAllocate(ReadOnlySpan<byte> path, int mode, ulong offset, long length, ref FuseFileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual int Flush(ReadOnlySpan<byte> path, ref FuseFileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual int FSync(ReadOnlySpan<byte> path, ref FuseFileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual int FSyncDir(ReadOnlySpan<byte> readOnlySpan, bool onlyData, ref FuseFileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual int GetAttr(ReadOnlySpan<byte> path, ref Stat stat, FuseFileInfoRef fiRef)
            => FuseConstants.ENOSYS;

        public virtual int GetXAttr(ReadOnlySpan<byte> path, ReadOnlySpan<byte> name, Span<byte> data)
            => FuseConstants.ENOSYS;

        public virtual int Link(ReadOnlySpan<byte> fromPath, ReadOnlySpan<byte> toPath)
            => FuseConstants.ENOSYS;

        public virtual int ListXAttr(ReadOnlySpan<byte> path, Span<byte> list)
            => FuseConstants.ENOSYS;

        public virtual int MkDir(ReadOnlySpan<byte> path, uint mode)
            => FuseConstants.ENOSYS;

        public virtual int Open(ReadOnlySpan<byte> path, ref FuseFileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual int OpenDir(ReadOnlySpan<byte> path, ref FuseFileInfo fi)
            => 0;

        public virtual int Read(ReadOnlySpan<byte> path, ulong offset, Span<byte> buffer, ref FuseFileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual int ReadDir(ReadOnlySpan<byte> path, ulong offset, ReadDirFlags flags, DirectoryContent content, ref FuseFileInfo fi)
            => FuseConstants.ENOSYS;

        public virtual int ReadLink(ReadOnlySpan<byte> path, Span<byte> buffer)
            => FuseConstants.ENOSYS;

        public virtual void Release(ReadOnlySpan<byte> path, ref FuseFileInfo fi)
        { }

        public virtual int ReleaseDir(ReadOnlySpan<byte> path, ref FuseFileInfo fi)
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

        public virtual int Truncate(ReadOnlySpan<byte> path, ulong length, FuseFileInfoRef fiRef)
            => FuseConstants.ENOSYS;

        public virtual int Unlink(ReadOnlySpan<byte> path)
            => FuseConstants.ENOSYS;

        public virtual int UpdateTimestamps(ReadOnlySpan<byte> path, ref TimeSpec atime, ref TimeSpec mtime, FuseFileInfoRef fiRef)
            => FuseConstants.ENOSYS;

        public virtual int Write(ReadOnlySpan<byte> path, ulong off, ReadOnlySpan<byte> span, ref FuseFileInfo fi)
            => FuseConstants.ENOSYS;
    }
}