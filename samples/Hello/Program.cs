using System;
using System.IO;
using Tmds.Fuse;

namespace Hello
{
    class Program
    {
        static void Main(string[] args)
        {
            const string mountPoint = "/tmp/hellofs";
            System.Console.WriteLine($"Mounting filesystem at {mountPoint}");

            Fuse.TryUnmount(mountPoint);

            // Ensure mount point directory exists
            Directory.CreateDirectory(mountPoint);

            try
            {
                Fuse.Mount(mountPoint, new MyFileSystem());
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
