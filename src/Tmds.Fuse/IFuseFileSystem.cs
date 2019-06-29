using System;
using System.Runtime.InteropServices;
using System.Text;
using Tmds.Linux;

namespace Tmds.Fuse
{
    public interface IFuseFileSystem : IDisposable
    {
        bool SupportsMultiThreading { get; }

        int GetAttr(ReadOnlySpan<char> path, ref stat stat, FuseFileInfoRef fiRef);
        int Chown(ReadOnlySpan<char> path, uint uid, uint gid, FuseFileInfoRef fiRef);
        int Open(ReadOnlySpan<char> path, ref FuseFileInfo fi);
        void Release(ReadOnlySpan<char> path, ref FuseFileInfo fi);
        int Rename(ReadOnlySpan<char> path, ReadOnlySpan<char> newPath, int flags);
        int Read(ReadOnlySpan<char> path, ulong offset, Span<byte> buffer, ref FuseFileInfo fi);
        int ReadDir(ReadOnlySpan<char> path, ulong offset, ReadDirFlags flags, DirectoryContent content, ref FuseFileInfo fi);
        int SymLink(ReadOnlySpan<char> path, ReadOnlySpan<char> target);
        int RmDir(ReadOnlySpan<char> path);
        int Unlink(ReadOnlySpan<char> path);
        int MkDir(ReadOnlySpan<char> path, mode_t mode);
        int Create(ReadOnlySpan<char> path, mode_t mode, ref FuseFileInfo fi);
        int ReadLink(ReadOnlySpan<char> path, Span<byte> buffer);
        int Truncate(ReadOnlySpan<char> path, ulong length, FuseFileInfoRef fiRef);
        int Write(ReadOnlySpan<char> path, ulong offset, ReadOnlySpan<byte> buffer, ref FuseFileInfo fi);
        int StatFS(ReadOnlySpan<char> path, ref statvfs statfs);
        int ChMod(ReadOnlySpan<char> path, mode_t mode, FuseFileInfoRef fiRef);
        int Link(ReadOnlySpan<char> fromPath, ReadOnlySpan<char> toPath);
        int UpdateTimestamps(ReadOnlySpan<char> path, ref timespec atime, ref timespec mtime, FuseFileInfoRef fiRef);
        int Flush(ReadOnlySpan<char> path, ref FuseFileInfo fi);
        int FSync(ReadOnlySpan<char> path, ref FuseFileInfo fi);
        int SetXAttr(ReadOnlySpan<char> path, ReadOnlySpan<char> name, ReadOnlySpan<byte> data, int flags);
        int GetXAttr(ReadOnlySpan<char> path, ReadOnlySpan<char> name, Span<byte> data);
        int ListXAttr(ReadOnlySpan<char> path, Span<byte> list);
        int RemoveXAttr(ReadOnlySpan<char> path, ReadOnlySpan<char> name);
        int OpenDir(ReadOnlySpan<char> path, ref FuseFileInfo fi);
        int ReleaseDir(ReadOnlySpan<char> path, ref FuseFileInfo fi);
        int FSyncDir(ReadOnlySpan<byte> readOnlySpan, bool onlyData, ref FuseFileInfo fi);
        int Access(ReadOnlySpan<char> path, mode_t mode);
        int FAllocate(ReadOnlySpan<char> path, int mode, ulong offset, long length, ref FuseFileInfo fi);
    }
}