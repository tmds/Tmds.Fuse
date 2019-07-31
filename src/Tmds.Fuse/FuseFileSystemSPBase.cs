using System;
using System.Runtime.InteropServices;
using System.Text;
using static Tmds.Linux.LibC;
using Tmds.Linux;

namespace Tmds.Fuse
{
    public class FuseFileSystemSPBase : IFuseFileSystemSP
    {
        public static string RootPath => "/";

        public virtual bool SupportsMultiThreading => false;

        public virtual int Access(string path, mode_t mode)
            => -ENOSYS;

        public virtual int ChMod(string path, mode_t mode, FuseFileInfoRef fiRef)
            => -ENOSYS;

        public virtual int Chown(string path, uint uid, uint gid, FuseFileInfoRef fiRef)
            => -ENOSYS;

        public virtual int Create(string path, mode_t mode, ref FuseFileInfo fi)
            => -ENOSYS;

        public virtual void Dispose()
        { }

        public virtual int FAllocate(string path, int mode, ulong offset, long length, ref FuseFileInfo fi)
            => -ENOSYS;

        public virtual int Flush(string path, ref FuseFileInfo fi)
            => -ENOSYS;

        public virtual int FSync(string path, ref FuseFileInfo fi)
            => -ENOSYS;

        public virtual int FSyncDir(ReadOnlySpan<byte> readOnlySpan, bool onlyData, ref FuseFileInfo fi)
            => -ENOSYS;

        public virtual int GetAttr(string path, ref stat stat, FuseFileInfoRef fiRef)
            => -ENOSYS;

        public virtual int GetXAttr(string path, string name, Span<byte> data)
            => -ENOSYS;

        public virtual int Link(string fromPath, string toPath)
            => -ENOSYS;

        public virtual int ListXAttr(string path, Span<byte> list)
            => -ENOSYS;

        public virtual int MkDir(string path, mode_t mode)
            => -ENOSYS;

        public virtual int Open(string path, ref FuseFileInfo fi)
            => -ENOSYS;

        public virtual int OpenDir(string path, ref FuseFileInfo fi)
            => 0;

        public virtual int Read(string path, ulong offset, Span<byte> buffer, ref FuseFileInfo fi)
            => -ENOSYS;

        public virtual int ReadDir(string path, ulong offset, ReadDirFlags flags, DirectoryContent content, ref FuseFileInfo fi)
            => -ENOSYS;

        public virtual int ReadLink(string path, Span<byte> buffer)
            => -ENOSYS;

        public virtual void Release(string path, ref FuseFileInfo fi)
        { }

        public virtual int ReleaseDir(string path, ref FuseFileInfo fi)
            => -ENOSYS;

        public virtual int RemoveXAttr(string path, string name)
            => -ENOSYS;
        public virtual int Rename(string path, string newPath, int flags)
            => -ENOSYS;

        public virtual int RmDir(string path)
            => -ENOSYS;

        public virtual int SetXAttr(string path, string name, ReadOnlySpan<byte> data, int flags)
            => -ENOSYS;

        public virtual int StatFS(string path, ref statvfs statfs)
            => -ENOSYS;

        public virtual int SymLink(string path, string target)
            => -ENOSYS;

        public virtual int Truncate(string path, ulong length, FuseFileInfoRef fiRef)
            => -ENOSYS;

        public virtual int Unlink(string path)
            => -ENOSYS;

        public virtual int UpdateTimestamps(string path, ref timespec atime, ref timespec mtime, FuseFileInfoRef fiRef)
            => -ENOSYS;

        public virtual int Write(string path, ulong off, ReadOnlySpan<byte> span, ref FuseFileInfo fi)
            => -ENOSYS;
    }
}