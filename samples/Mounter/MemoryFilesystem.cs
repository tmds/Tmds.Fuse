using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.IO;
using Tmds.Fuse;
using static Tmds.Fuse.FuseConstants;

namespace Mounter
{
    class MemoryFileSystem : FuseFileSystemBase
    {
        struct EntryName : IEquatable<EntryName>
        {
            private byte[] _name;

            public EntryName(byte[] name)
                => _name = name;

            public bool Equals(EntryName other)
                => new Span<byte>(_name).SequenceEqual(other._name);

            public override bool Equals(object obj)
            {
                if (obj is EntryName name)
                {
                    return name.Equals(this);
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode() => Hash.GetFNVHashCode(_name);

            public static implicit operator EntryName(byte[] name) => new EntryName(name);

            public static implicit operator EntryName(ReadOnlySpan<byte> name) => new EntryName(name.ToArray());

            public static implicit operator ReadOnlySpan<byte>(EntryName name) => name._name;
        }

        class Entry
        {
            private int _refCount;

            public int RefCount => _refCount;
            public int Mode { get; set; }
            public DateTime ATime { get; set; }
            public DateTime MTime { get; set; }

            public Entry()
                => _refCount = 1;

            public void RefCountInc()
            {
                if (_refCount == 0)
                {
                    throw new InvalidOperationException();
                }

                _refCount++;
            }

            public void RefCountDec()
            {
                if (_refCount == 0)
                {
                    throw new InvalidOperationException();
                }

                _refCount -= 1;

                if (_refCount == 0)
                {
                    DisposeEntry();
                }
            }

            protected virtual void DisposeEntry()
            { }
        }

        class File : Entry
        {
            private readonly Stream _content;

            public File(Stream content, int mode)
            {
                _content = content;
                Mode = mode;
            }

            public int Size => (int)_content.Length;

            public int Read(ulong offset, Span<byte> buffer)
            {
                if (offset > (ulong)_content.Length)
                {
                    return 0;
                }
                _content.Position = (long)offset;
                return _content.Read(buffer);
            }

            public int Truncate(ulong length)
            {
                // Do we support this size?
                if (length > int.MaxValue)
                {
                    return EINVAL;
                }

                _content.SetLength((long)length);

                return 0;
            }

            public int Write(ulong offset, ReadOnlySpan<byte> buffer)
            {
                // Do we support this size?
                ulong newLength = offset + (ulong)buffer.Length;
                if (newLength > int.MaxValue || offset > int.MaxValue)
                {
                    return EFBIG;
                }

                // Copy the data
                _content.Position = (long)offset;
                _content.Write(buffer);
                return buffer.Length;
            }

            protected override void DisposeEntry()
            {
                _content.Dispose();
            }
        }

        class Directory : Entry
        {
            public Dictionary<EntryName, Entry> Entries { get; } = new Dictionary<EntryName, Entry>();

            public Directory(int mode)
            {
                Mode = mode;
            }

            public Entry FindEntry(ReadOnlySpan<byte> path)
            {
                while (path.Length > 0 && path[0] == (byte)'/')
                {
                    path = path.Slice(1);
                }
                if (path.Length == 0)
                {
                    return this;
                }
                int endOfName = path.IndexOf((byte)'/');
                bool directChild = endOfName == -1;
                ReadOnlySpan<byte> name = directChild ? path : path.Slice(0, endOfName);
                if (Entries.TryGetValue(name, out Entry value))
                {
                    if (directChild)
                    {
                        return value;
                    }
                    else
                    {
                        Directory dir = value as Directory;
                        if (dir == null)
                        {
                            return null;
                        }
                        return dir.FindEntry(path.Slice(endOfName + 1));
                    }
                }
                else
                {
                    return null;
                }
            }

            public Directory AddDirectory(string name, int mode)
                => AddDirectory(Encoding.UTF8.GetBytes(name), mode);

            public Directory AddDirectory(ReadOnlySpan<byte> name, int mode)
            {
                Directory directory = null;
                try
                {
                    directory = new Directory(mode);
                    AddEntry(name, directory);
                    RefCountInc(); // subdirs link to their parent
                    return directory;
                }
                finally
                {
                    directory?.RefCountDec();
                }
            }

            public File AddFile(string name, string content, int mode)
                => AddFile(Encoding.UTF8.GetBytes(name), Encoding.UTF8.GetBytes(content), mode);

            public File AddFile(ReadOnlySpan<byte> name, byte[] content, int mode)
            {
                File file = null;
                try
                {
                    var memoryStream = new RecyclableMemoryStream(MemoryFileSystem.MemoryManager);
                    memoryStream.Write(content);
                    file = new File(memoryStream, mode);
                    AddEntry(name, file);
                    return file;
                }
                finally
                {
                    file?.RefCountDec();
                }
            }

            public void Remove(ReadOnlySpan<byte> name)
            {
                if (Entries.Remove(name, out Entry entry))
                {
                    entry.RefCountDec();
                    if (entry is Directory)
                    {
                        RefCountDec(); // subdirs link to their parent
                    }
                }
            }

            public void AddEntry(ReadOnlySpan<byte> name, Entry entry)
            {
                Entries.Add(name, entry);
                entry.RefCountInc();
            }

            // This is called when the FileSystem is Disposed
            protected void DisposeDirectory()
            {
                // Do a recursive 'remove' of all entries
                // causing memory to be returned to the MemoryManager.
                while (Entries.Count != 0)
                {
                    (EntryName name, Entry entry) = Entries.First();
                    if (entry is Directory dir)
                    {
                        dir.DisposeDirectory();
                    }
                    Remove(name);
                }
            }
        }

        class RootDirectory : Directory, IDisposable
        {
            public RootDirectory(int mode) :
                base(mode)
            {}

            public void Dispose()
                => DisposeDirectory();
        }

        class OpenFile
        {
            private readonly File _file;

            public OpenFile(File file)
                => _file = file;

            public int Mode
            {
                get => _file.Mode;
                set => _file.Mode = value;
            }
            public Entry Entry => _file;

            public int Read(ulong offset, Span<byte> buffer)
                => _file.Read(offset, buffer);

            public void Truncate(ulong offset)
                => _file.Truncate(offset);

            public int Write(ulong offset, ReadOnlySpan<byte> buffer)
                => _file.Write(offset, buffer);
        }

        public virtual void Dispose() => _root.Dispose();

        // TODO: inform fuse the implementation is not thread-safe.
        public MemoryFileSystem()
        {
            _root =  new RootDirectory(0b111_101_101);

            const int defaultFileMode = 0b100_100_100;  // r--r--r--
            const int defaultDirMode  = 0b111_101_101; // rwxr-xr-x
            // Add some stuff.
            _root.AddFile("file1", "Content of file1", defaultFileMode);
            Directory sampleDir = _root.AddDirectory("empty_dir", defaultDirMode);
            Directory dirWithFiles = _root.AddDirectory("dir_with_files", defaultDirMode);
            dirWithFiles.AddFile("file2", "Content of file2", defaultFileMode);
            dirWithFiles.AddFile("file3", "Content of file3", defaultFileMode);
            Directory nestedDir = dirWithFiles.AddDirectory("nested_dir", defaultDirMode);
            nestedDir.AddFile("file4", "Content of file4", defaultFileMode);
        }

        public override int GetAttr(ReadOnlySpan<byte> path, Stat stat, FuseFileInfo fi)
        {
            Entry entry = _root.FindEntry(path);
            if (entry == null)
            {
                return ENOENT;
            }
            stat.ATime = entry.ATime;
            stat.MTime = entry.MTime;
            stat.NLink = entry.RefCount;
            switch (entry)
            {
                case Directory directory:
                    stat.Mode = S_IFDIR | entry.Mode;
                    stat.NLink++; // add additional link for self ('.')
                    break;
                case File f:
                    stat.Mode = S_IFREG | entry.Mode;
                    stat.Size = f.Size;
                    break;
            }
            return 0;
        }

        public override int Read(ReadOnlySpan<byte> path, ulong offset, Span<byte> buffer, FuseFileInfo fi)
            => _openFiles[fi.FileDescriptor].Read(offset, buffer);

        public override int Write(ReadOnlySpan<byte> path, ulong offset, ReadOnlySpan<byte> buffer, FuseFileInfo fi)
            => _openFiles[fi.FileDescriptor].Write(offset, buffer);

        public override void Release(ReadOnlySpan<byte> path, FuseFileInfo fi)
            => _openFiles.Remove(fi.FileDescriptor);

        public override int Truncate(ReadOnlySpan<byte> path, ulong length, FuseFileInfo fi)
        {
            if (fi.FileDescriptor != 0)
            {
                _openFiles[fi.FileDescriptor].Truncate(length);
                return 0;
            }
            else
            {
                Entry entry = _root.FindEntry(path);
                if (entry == null)
                {
                    return ENOENT;
                }
                if (entry is File file)
                {
                    file.Truncate(length);
                    return 0;
                }
                else
                {
                    return EISDIR;
                }
            }
        }

        public override int ChMod(ReadOnlySpan<byte> path, int mode, FuseFileInfo fi)
        {
            if (!fi.IsNull && fi.FileDescriptor != 0)
            {
                _openFiles[fi.FileDescriptor].Mode = mode;
                return 0;
            }
            else
            {
                Entry entry = _root.FindEntry(path);
                if (entry == null)
                {
                    return ENOENT;
                }
                entry.Mode = mode;
                return 0;
            }
        }

        public override int MkDir(ReadOnlySpan<byte> path, int mode)
        {
            (Directory parent, bool parentIsNotDir, Entry entry) = FindParentAndEntry(path, out ReadOnlySpan<byte> name);
            if (parent == null)
            {
                return parentIsNotDir ? ENOTDIR : ENOENT;
            }
            if (entry != null)
            {
                return EEXIST;
            }

            parent.AddDirectory(name, mode);

            return 0;
        }

        public override int ReadDir(ReadOnlySpan<byte> path, ulong offset, ReadDirFlags flags, DirectoryContent content, FuseFileInfo fi)
        {
            Entry entry = _root.FindEntry(path);
            if (entry == null)
            {
                return ENOENT;
            }
            if (entry is Directory directory)
            {
                content.AddEntry(".");
                content.AddEntry("..");
                foreach (var child in directory.Entries)
                {
                    content.AddEntry(child.Key);
                }
                return 0;
            }
            else
            {
                return ENOTDIR;
            }
        }

        public override int Create(ReadOnlySpan<byte> path, int mode, FuseFileInfo fi)
        {
            (Directory parent, bool parentIsNotDir, Entry entry) = FindParentAndEntry(path, out ReadOnlySpan<byte> name);
            if (parent == null)
            {
                return parentIsNotDir ? ENOTDIR : ENOENT;
            }
            if (entry != null)
            {
                return EEXIST; // TODO, is this correct?
            }

            File newFile = parent.AddFile(name, Array.Empty<byte>(), mode);
            fi.FileDescriptor = FindFreeFileDescriptor(newFile);
            return 0;
        }

        public override int RmDir(ReadOnlySpan<byte> path)
        {
            (Directory parent, bool parentIsNotDir, Entry entry) = FindParentAndEntry(path, out ReadOnlySpan<byte> name);
            if (parent == null)
            {
                return parentIsNotDir ? ENOTDIR : ENOENT;
            }
            if (entry == null)
            {
                return ENOENT;
            }

            if (entry is Directory dir)
            {
                if (dir.Entries.Count != 0)
                {
                    return ENOTEMPTY;
                }

                parent.Remove(name);
                return 0;
            }
            else
            {
                return ENOTDIR;
            }
        }

        public override int Open(ReadOnlySpan<byte> path, FuseFileInfo fi)
        {
            (Directory parent, bool parentIsNotDir, Entry entry) = FindParentAndEntry(path, out ReadOnlySpan<byte> name);
            if (parent == null)
            {
                return parentIsNotDir ? ENOTDIR : ENOENT;
            }

            if (entry == null)
            {
                return ENOENT;
            }
            if (entry is File file)
            {
                if ((fi.Flags & O_TRUNC) != 0)
                {
                    file.Truncate(0);
                }
                fi.FileDescriptor = FindFreeFileDescriptor(file);
                return 0;
            }
            else
            {
                return EISDIR;
            }
        }

        private ulong FindFreeFileDescriptor(File file)
        {
            for (uint i = 1; i < uint.MaxValue; i++)
            {
                if (!_openFiles.ContainsKey(i))
                {
                    _openFiles[i] = new OpenFile(file);
                    return i;
                }
            }
            return ulong.MaxValue;
        }

        public override int Unlink(ReadOnlySpan<byte> path)
        {
            (Directory parent, bool parentIsNotDir, Entry entry) = FindParentAndEntry(path, out ReadOnlySpan<byte> name);
            if (parent == null)
            {
                return parentIsNotDir ? ENOTDIR : ENOENT;
            }
            if (entry == null)
            {
                return ENOENT;
            }

            if (entry is File file)
            {
                parent.Remove(name);
                return 0;
            }
            else
            {
                return EISDIR;
            }
        }

        public override int Link(ReadOnlySpan<byte> fromPath, ReadOnlySpan<byte> toPath)
        {
            Entry from = _root.FindEntry(fromPath);
            if (from == null)
            {
                return ENOENT;
            }
            (Directory parent, bool parentIsNotDir, Entry to) = FindParentAndEntry(toPath, out ReadOnlySpan<byte> name);
            if (parent == null)
            {
                return parentIsNotDir ? ENOTDIR : ENOENT;
            }
            if (to != null)
            {
                return EEXIST;
            }
            parent.AddEntry(name, from);
            return 0;
        }

        public override int UpdateTimestamps(ReadOnlySpan<byte> path, TimeSpec atime, TimeSpec mtime, FuseFileInfo fi)
        {
            Entry entry;
            if (!fi.IsNull && fi.FileDescriptor != 0)
            {
                entry = _openFiles[fi.FileDescriptor].Entry;
            }
            else
            {
                entry = _root.FindEntry(path);
            }
            if (entry == null)
            {
                return ENOENT;
            }
            DateTime now = atime.IsNow || mtime.IsNow ? DateTime.Now : DateTime.MinValue;
            if (!atime.IsOmit)
            {
                entry.ATime = atime.IsNow ? now : atime.ToDateTime();
            }
            if (!mtime.IsOmit)
            {
                entry.MTime = mtime.IsNow ? now : mtime.ToDateTime();
            }
            return 0;
        }

        private (Directory parent, bool parentIsNotDir, Entry entry) FindParentAndEntry(ReadOnlySpan<byte> path, out ReadOnlySpan<byte> name)
        {
            SplitPathIntoParentAndName(path, out ReadOnlySpan<byte> parentDir, out name);
            Entry entry = _root.FindEntry(parentDir);
            Directory parent = entry as Directory;
            bool parentIsNotDir;
            if (parent != null)
            {
                parentIsNotDir = false;
                entry = parent.FindEntry(name);
            }
            else
            {
                parentIsNotDir = true;
                entry = null;
            }
            return (parent, parentIsNotDir, entry);
        }

        private void SplitPathIntoParentAndName(ReadOnlySpan<byte> path, out ReadOnlySpan<byte> parent, out ReadOnlySpan<byte> name)
        {
            int separatorPos = path.LastIndexOf((byte)'/');
            parent = path.Slice(0, separatorPos);
            name = path.Slice(separatorPos + 1);
        }

        internal static RecyclableMemoryStreamManager MemoryManager = new RecyclableMemoryStreamManager(); // can this one be cleaned up?
        private readonly RootDirectory _root;
        private readonly Dictionary<ulong, OpenFile> _openFiles = new Dictionary<ulong, OpenFile>();
    }
}