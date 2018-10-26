using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Tmds.Fuse;
using static Tmds.Fuse.FuseConstants;

namespace Mounter
{
    class MemoryFileSystem : IFuseFileSystem
    {
        struct EntryName : IEquatable<EntryName>
        {
            private byte[] _name;
            public EntryName(byte[] name) => _name = name;

            public bool Equals(EntryName other)
            {
                return new Span<byte>(_name).SequenceEqual(other._name);
            }

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
            public static implicit operator ReadOnlySpan<byte>(EntryName name) => name._name;
        }

        interface IEntry
        { }

        class File : IEntry
        {
            private byte[] _content;

            public File(byte[] content)
            {
                _content = content;
            }

            public int Size => _content.Length;

            internal int Read(ReadOnlySpan<byte> path, ulong offset, Span<byte> buffer)
            {
                if (offset > (ulong)_content.Length)
                {
                    return 0;
                }
                int intOffset = (int)offset;
                int length = (int)Math.Min(_content.Length - intOffset, buffer.Length);
                _content.AsSpan().Slice(intOffset, length).CopyTo(buffer);
                return length;
            }
        }

        class Directory : IEntry
        {
            public Dictionary<EntryName, IEntry> Entries { get; } = new Dictionary<EntryName, IEntry>();

            public IEntry FindEntry(ReadOnlySpan<byte> path)
            {
                (IEntry entry, bool created) = FindEntry(path, null);
                return entry;
            }

            public (IEntry entry, bool created) FindEntry(ReadOnlySpan<byte> path, Func<IEntry> createEntry)
            {
                while (path.Length > 0 && path[0] == (byte)'/')
                {
                    path = path.Slice(1);
                }
                if (path.Length == 0)
                {
                    return (this, false);
                }
                int endOfName = path.IndexOf((byte)'/');
                bool directChild = endOfName == -1;
                ReadOnlySpan<byte> name = directChild ? path : path.Slice(0, endOfName);
                if (Entries.TryGetValue(name.ToArray(), out IEntry value))
                {
                    if (directChild)
                    {
                        return (value, false);
                    }
                    else
                    {
                        Directory dir = value as Directory;
                        if (dir == null)
                        {
                            return (null, false);
                        }
                        return dir.FindEntry(path.Slice(endOfName + 1), createEntry);
                    }
                }
                else
                {
                    if (directChild)
                    {
                        if (createEntry != null)
                        {
                            IEntry newEntry = createEntry();
                            Entries[name.ToArray()] = newEntry;
                            return (newEntry, true);
                        }
                        else
                        {
                            return (null, false);
                        }
                    }
                    else
                    {
                        return (null, false);
                    }
                }
            }

            internal Directory AddDirectory(string name)
            {
                var directory = new Directory();
                Entries.Add(Encoding.UTF8.GetBytes(name), directory);
                return directory;
            }

            internal void AddFile(string name, string content)
            {
                Entries.Add(Encoding.UTF8.GetBytes(name), new File(Encoding.UTF8.GetBytes(content)));
            }
        }

        // TODO: inform fuse the implementation is not thread-safe.
        public MemoryFileSystem()
        {
            _root.AddFile("file1", "Content of file1");
            Directory sampleDir = _root.AddDirectory("empty_dir");
            Directory dirWithFiles = _root.AddDirectory("dir_with_files");
            dirWithFiles.AddFile("file2", "Content of file2");
            dirWithFiles.AddFile("file3", "Content of file3");
            Directory nestedDir = dirWithFiles.AddDirectory("nested_dir");
            nestedDir.AddFile("file4", "Content of file4");
        }


        public int GetAttr(ReadOnlySpan<byte> path, Stat stat, FileInfo fi)
        {
            IEntry entry = _root.FindEntry(path);
            switch (entry)
            {
                case null:
                    return ENOENT;
                case Directory directory:
                    stat.Mode = S_IFDIR | 0b111_101_101; // rwxr-xr-x
                    int dirCount = 0;
                    foreach (var child in directory.Entries)
                    {
                        if (child is Directory) dirCount++;
                    }
                    stat.NLink = 2 + dirCount; // TODO: do we really need this??
                    break;
                case File f:
                    stat.Mode = S_IFREG | 0b100_100_100; // r--r--r--
                    stat.NLink = 1;
                    stat.Size = f.Size;
                    break;
            }
            return 0;
        }

        public int Open(ReadOnlySpan<byte> path, FileInfo fi)
        {
            IEntry entry = _root.FindEntry(path);
            if (entry == null)
            {
                return ENOENT;
            }
            if (entry is File file)
            {
                return 0;
            }
            else
            {
                return EISDIR;
            }
        }

        public int Read(ReadOnlySpan<byte> path, ulong offset, Span<byte> buffer, FileInfo fi)
        {
            IEntry entry = _root.FindEntry(path);
            if (entry == null)
            {
                return ENOENT;
            }
            if (entry is File file)
            {
                return file.Read(path, offset, buffer);
            }
            else
            {
                return EISDIR;
            }
        }

        public int ReadDir(ReadOnlySpan<byte> path, ulong offset, ReadDirFlags flags, DirectoryContent content, FileInfo fi)
        {
            IEntry entry = _root.FindEntry(path);
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

        private readonly Directory _root = new Directory();
    }
}