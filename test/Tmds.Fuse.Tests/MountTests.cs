using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using static Tmds.Fuse.FuseConstants;

namespace Tmds.Fuse.Tests
{
    public class UnitTest1
    {
        class DummyFileSystem : FuseFileSystemBase
        {
            public override int GetAttr(ReadOnlySpan<byte> path, ref Stat stat, FuseFileInfoRef fiRef)
            {
                stat.st_nlink = 1;
                stat.st_mode = S_IFREG;
                return 0;
            }

            public override int Open(ReadOnlySpan<byte> path, ref FuseFileInfo fi)
                => 0;

            public int DisposeCount { get; set; }

            public override void Dispose()
            {
                DisposeCount++;
            }
        }

        [Fact]
        public void MountFail_DisposesFileSystem_And_ThrowsFuseException()
        {
            DummyFileSystem dummyFileSystem = new DummyFileSystem();
            Assert.Throws<FuseException>(() => Fuse.Mount("/tmp/no_such_mountpoint", dummyFileSystem));
            Assert.Equal(1, dummyFileSystem.DisposeCount);
        }

        [Fact]
        public async Task Unmount_DisposesFileSystem()
        {
            DummyFileSystem dummyFileSystem = new DummyFileSystem();
            string mountPoint = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            Directory.CreateDirectory(mountPoint);

            IFuseMount mount = Fuse.Mount(mountPoint, dummyFileSystem);

            mount.LazyUnmount();

            await mount.WaitForUnmountAsync();

            Assert.Equal(1, dummyFileSystem.DisposeCount);
        }
    }
}
