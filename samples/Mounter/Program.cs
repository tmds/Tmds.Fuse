using System;
using System.IO;
using Tmds.Fuse;

namespace Mounter
{
    class Program
    {
        static void Main(string[] args)
        {
            string type = args.Length > 0 ? args[0] : "hello";

            IFuseFileSystem fileSystem;
            if (type == "hello")
            {
                fileSystem = new HelloFileSystem();
            }
            else if (type == "memory")
            {
                fileSystem = new MemoryFileSystem();
            }
            else if (type == "pokemon")
            {
                fileSystem = new PokemonFileSystem();
            }
            else
            {
                System.Console.WriteLine("Unknown file system type");
                return;
            }

            string mountPoint = $"/tmp/{type}fs";
            System.Console.WriteLine($"Mounting filesystem at {mountPoint}");

            Fuse.TryUnmount(mountPoint);

            // Ensure mount point directory exists
            Directory.CreateDirectory(mountPoint);

            try
            {
                Fuse.Mount(mountPoint, fileSystem);
            }
            catch (FuseException fe)
            {
                Console.WriteLine($"Fuse throw an exception: {fe}");

                Console.WriteLine("Try unmounting the file system by executing:");
                Console.WriteLine($"fuser -kM {mountPoint}");
                Console.WriteLine($"sudo umount -f {mountPoint}");
            }
        }
    }
}
