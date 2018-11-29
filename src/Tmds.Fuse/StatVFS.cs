namespace Tmds.Fuse
{
    public unsafe struct StatVFS
    {
        public ulong b_size { get; set; }
        public ulong f_frsize { get; set; }
        public ulong f_blocks { get; set; }
        public ulong f_bree { get; set; }
        public ulong f_bavail { get; set; }
        public ulong f_files { get; set; }
        public ulong f_ffree { get; set; }
        public ulong f_favail { get; set; }
        public ulong f_fsid { get; set; }
        public ulong f_flag { get; set; }
        public ulong f_namemax { get; set; }
        private fixed int __f_spare[5];
    }
}