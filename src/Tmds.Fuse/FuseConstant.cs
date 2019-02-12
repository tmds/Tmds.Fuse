using System;

namespace Tmds.Fuse
{
    public static class FuseConstants
    {
        private static byte[] _rootPath = new byte[] { (byte)'/' };
        public static ReadOnlySpan<byte> RootPath => _rootPath;
    }
}