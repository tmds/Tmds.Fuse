using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Tmds.Fuse
{
    public interface IFuseFileSystem : IDisposable
    {
        bool SupportsMultiThreading { get; }

        int GetAttr(ReadOnlySpan<byte> path, ref Stat stat, FuseFileInfoRef fiRef);
        int Chown(ReadOnlySpan<byte> path, uint uid, uint gid, FuseFileInfoRef fiRef);
        int Open(ReadOnlySpan<byte> path, ref FuseFileInfo fi);
        void Release(ReadOnlySpan<byte> path, ref FuseFileInfo fi);
        int Rename(ReadOnlySpan<byte> path, ReadOnlySpan<byte> newPath, int flags);
        int Read(ReadOnlySpan<byte> path, ulong offset, Span<byte> buffer, ref FuseFileInfo fi);
        int ReadDir(ReadOnlySpan<byte> path, ulong offset, ReadDirFlags flags, DirectoryContent content, ref FuseFileInfo fi);
        int SymLink(ReadOnlySpan<byte> path, ReadOnlySpan<byte> target);
        int RmDir(ReadOnlySpan<byte> path);
        int Unlink(ReadOnlySpan<byte> path);
        int MkDir(ReadOnlySpan<byte> path, uint mode);
        int Create(ReadOnlySpan<byte> path, uint mode, ref FuseFileInfo fi);
        int ReadLink(ReadOnlySpan<byte> path, Span<byte> buffer);
        int Truncate(ReadOnlySpan<byte> path, ulong length, FuseFileInfoRef fiRef);
        int Write(ReadOnlySpan<byte> path, ulong offset, ReadOnlySpan<byte> buffer, ref FuseFileInfo fi);
        int StatFS(ReadOnlySpan<byte> path, ref StatVFS statfs);
        int ChMod(ReadOnlySpan<byte> path, uint mode, FuseFileInfoRef fiRef);
        int Link(ReadOnlySpan<byte> fromPath, ReadOnlySpan<byte> toPath);
        int UpdateTimestamps(ReadOnlySpan<byte> path, ref TimeSpec atime, ref TimeSpec mtime, FuseFileInfoRef fiRef);
        int Flush(ReadOnlySpan<byte> path, ref FuseFileInfo fi);
        int FSync(ReadOnlySpan<byte> path, ref FuseFileInfo fi);
        int SetXAttr(ReadOnlySpan<byte> path, ReadOnlySpan<byte> name, ReadOnlySpan<byte> data, int flags);
        int GetXAttr(ReadOnlySpan<byte> path, ReadOnlySpan<byte> name, Span<byte> data);
        int ListXAttr(ReadOnlySpan<byte> path, Span<byte> list);
        int RemoveXAttr(ReadOnlySpan<byte> path, ReadOnlySpan<byte> name);
        int OpenDir(ReadOnlySpan<byte> path, ref FuseFileInfo fi);
        int ReleaseDir(ReadOnlySpan<byte> path, ref FuseFileInfo fi);
        int FSyncDir(ReadOnlySpan<byte> readOnlySpan, bool onlyData, ref FuseFileInfo fi);
        int Access(ReadOnlySpan<byte> path, uint mode);
        int FAllocate(ReadOnlySpan<byte> path, int mode, ulong offset, long length, ref FuseFileInfo fi);
    }
}