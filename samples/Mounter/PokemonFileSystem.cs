using System;
using System.Net.Http;
using Tmds.Fuse;
using static Tmds.Fuse.FuseConstants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Collections.Generic;
using System.Net;

namespace Mounter
{
    class PokemonFileSystem : FuseFileSystemBase
    {
        private class OpenFile
        {
            public OpenFile(byte[] data)
                => Data = data;

            public byte[] Data { get; }
        }

        private readonly HttpClient _httpClient;
        private readonly Dictionary<ulong, OpenFile> _openFiles = new Dictionary<ulong, OpenFile>();
        private ulong _nextFd;

        public PokemonFileSystem()
        {
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri("https://pokeapi.co/api/v2/pokemon/")
            };
        }

        public override void Dispose() => _httpClient.Dispose();

        public override int GetAttr(ReadOnlySpan<byte> path, Stat stat, FileInfo fi)
        {
            if (path.SequenceEqual(RootPath))
            {
                stat.Mode = S_IFDIR | 0b111_101_101; // rwxr-xr-x
                stat.NLink = 2; // 2 + nr of subdirectories
                return 0;
            }
            else
            {
                stat.Mode = S_IFREG | 0b100_100_100; // r--r--r--
                stat.NLink = 1;
                return 0;
            }
        }

        public override int ReadDir(ReadOnlySpan<byte> path, ulong offset, ReadDirFlags flags, DirectoryContent content, FileInfo fi)
        {
            if (!path.SequenceEqual(RootPath))
            {
                return ENOENT;
            }

            content.AddEntry(".");
            content.AddEntry("..");
            foreach (var pokemon in GetAsJson("")["results"])
            {
                content.AddEntry((string)pokemon["name"]);
            }
            return 0;
        }

        public override int Read(ReadOnlySpan<byte> path, ulong offset, Span<byte> buffer, FileInfo fi) // TODO: rename to FuseFileInfo
        {
            byte[] data = _openFiles[fi.FileDescriptor].Data;
            if (offset > (ulong)data.Length)
            {
                return 0;
            }
            int intOffset = (int)offset;
            int length = (int)Math.Min(data.Length - intOffset, buffer.Length);
            data.AsSpan().Slice(intOffset, length).CopyTo(buffer);
            return length;
        }

        public override void Release(ReadOnlySpan<byte> path, FileInfo fi)
        {
            _openFiles.Remove(fi.FileDescriptor);
        }

        public override int Open(ReadOnlySpan<byte> path, FileInfo fi)
        {
            string name = Encoding.UTF8.GetString(path.Slice(1)) + "/";
            byte[] data = GetAsBytes(name);
            if (data == null)
            {
                return ENOENT;
            }

            ulong fd = FindFreeFd();
            fi.FileDescriptor = fd;
            fi.DirectIO = true; // GetAttr doesn't return the actual size.
            _openFiles.Add(fd, new OpenFile(data));

            return 0;
        }

        private ulong FindFreeFd()
        {
            while (true)
            {
                ulong fd = unchecked(_nextFd++);
                if (!_openFiles.ContainsKey(fd))
                {
                    return fd;
                }
            }
        }

        private JObject GetAsJson(string path)
        {
            var response = GetAsResponseMessage(path);
            var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            var serializer = new JsonSerializer();
            using (var sr = new System.IO.StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                return serializer.Deserialize(jsonTextReader) as JObject;
            }
        }

        private byte[] GetAsBytes(string path)
        {
            using (HttpResponseMessage response = GetAsResponseMessage(path))
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                }
                else
                {
                    return null;
                }
            }
        }

        private HttpResponseMessage GetAsResponseMessage(string path)
            => _httpClient.GetAsync(path).GetAwaiter().GetResult();
    }
}