using System;
using System.Runtime.InteropServices;
using System.Text;
using Tmds.Linux;

namespace Tmds.Fuse
{
    public interface IFuseFileSystemSP : IDisposable
    {
        bool SupportsMultiThreading { get; }

        int GetAttr(string path, ref stat stat, FuseFileInfoRef fiRef);
        int Chown(string path, uint uid, uint gid, FuseFileInfoRef fiRef);
        int Open(string path, ref FuseFileInfo fi);
        void Release(string path, ref FuseFileInfo fi);
        int Rename(string path, string newPath, int flags);
        int Read(string path, ulong offset, Span<byte> buffer, ref FuseFileInfo fi);
        int ReadDir(string path, ulong offset, ReadDirFlags flags, DirectoryContent content, ref FuseFileInfo fi);
        int SymLink(string path, string target);
        int RmDir(string path);
        int Unlink(string path);
        int MkDir(string path, mode_t mode);
        int Create(string path, mode_t mode, ref FuseFileInfo fi);
        int ReadLink(string path, Span<byte> buffer);
        int Truncate(string path, ulong length, FuseFileInfoRef fiRef);
        int Write(string path, ulong offset, ReadOnlySpan<byte> buffer, ref FuseFileInfo fi);
        int StatFS(string path, ref statvfs statfs);
        int ChMod(string path, mode_t mode, FuseFileInfoRef fiRef);
        int Link(string fromPath, string toPath);
        int UpdateTimestamps(string path, ref timespec atime, ref timespec mtime, FuseFileInfoRef fiRef);
        int Flush(string path, ref FuseFileInfo fi);
        int FSync(string path, ref FuseFileInfo fi);
        int SetXAttr(string path, string name, ReadOnlySpan<byte> data, int flags);
        int GetXAttr(string path, string name, Span<byte> data);
        int ListXAttr(string path, Span<byte> list);
        int RemoveXAttr(string path, string name);
        int OpenDir(string path, ref FuseFileInfo fi);
        int ReleaseDir(string path, ref FuseFileInfo fi);
        int FSyncDir(ReadOnlySpan<byte> readOnlySpan, bool onlyData, ref FuseFileInfo fi);
        int Access(string path, mode_t mode);
        int FAllocate(string path, int mode, ulong offset, long length, ref FuseFileInfo fi);
    }
}