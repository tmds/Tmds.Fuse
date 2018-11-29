namespace Tmds.Fuse
{
    // This matches the layout of 'struct stat' on linux-x64
    public unsafe struct Stat
    {
        public ulong st_dev { get; set; }
        public ulong st_ino { get; set; }
        public ulong st_nlink { get; set; }
        public uint st_mode { get; set; }
        public uint st_uid { get; set; }
        public uint st_gid { get; set; }
        private int __pad0;
        public ulong st_rdev { get; set; }
        public long st_size { get; set; }
        public long st_blksize { get; set; }
        public long st_blocks { get; set; }
        public TimeSpec st_atim { get; set; }
        public TimeSpec st_mtim { get; set; }
        public TimeSpec st_ctim { get; set; }
        private fixed long __glib_reserved[2]; // TODO: should this be 3??
    }
}