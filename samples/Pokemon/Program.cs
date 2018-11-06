using System;
using System.Net.Http;
using Tmds.Fuse;
using static Tmds.Fuse.FuseConstants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Pokemon
{
    class PokemonFileSystem : FuseFileSystemBase // TODO: IDisposable
    {
        private static readonly byte[] _rootPath = Encoding.UTF8.GetBytes("/"); // TODO: add to FuseConstants
        private readonly HttpClient _httpClient;
        public PokemonFileSystem()
        {
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri("https://pokeapi.co/api/v2/pokemon/")
            };
        }

        public override int GetAttr(ReadOnlySpan<byte> path, Stat stat, FileInfo fi)
        {
            if (path.SequenceEqual(_rootPath))
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
            if (!path.SequenceEqual(_rootPath))
            {
                return ENOENT;
            }
            try
            {
                content.AddEntry(".");
                content.AddEntry("..");
                foreach (var pokemon in GetAsJson("")["results"])
                {
                    content.AddEntry((string)pokemon["name"]);
                }
                return 0;
            }
            catch (Exception e) // TODO: move up
            {
                return EIO;
            }
        }

        public override int Read(ReadOnlySpan<byte> path, ulong offset, Span<byte> buffer, FileInfo fi) // TODO: rename to FuseFileInfo
        {
            try
            {
                string name = Encoding.UTF8.GetString(path.Slice(1));
                byte[] data = GetAsBytes(name);
                if (offset > (ulong)data.Length)
                {
                    return 0;
                }
                int intOffset = (int)offset;
                int length = (int)Math.Min(data.Length - intOffset, buffer.Length);
                data.AsSpan().Slice(intOffset, length).CopyTo(buffer);
                return length;
            }
            catch
            {
                return EIO;
            }
        }

        public JObject GetAsJson(string path)
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

        private HttpResponseMessage GetAsResponseMessage(string path)
            => _httpClient.GetAsync(path).GetAwaiter().GetResult();

        public byte[] GetAsBytes(string path)
            => GetAsResponseMessage(path).Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
    }

    class Program
    {
        static void Main(string[] args)
        {
            Fuse.Mount("/tmp/pokemon", new PokemonFileSystem());
        }
    }
}
