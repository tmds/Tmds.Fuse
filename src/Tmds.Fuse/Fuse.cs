using System;
using System.Diagnostics;

namespace Tmds.Fuse
{
    public static class Fuse
    {
        public static void Mount(string mountPoint, IFuseFileSystem fileSystem)
        {
            FuseMount mount = new FuseMount(mountPoint, fileSystem);
            mount.Mount();
        }

        public static void TryUnmount(string mountPoint)
        {
            // we need root to unmount
            // fusermount runs as root (setuid)
            var psi = new ProcessStartInfo
            {
                FileName = "fusermount",
                Arguments = $"-u {mountPoint}",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var process = Process.Start(psi))
            {
                process.WaitForExit();
            }
        }
    }
}