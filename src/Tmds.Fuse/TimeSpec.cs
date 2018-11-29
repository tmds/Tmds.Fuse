using System;

namespace Tmds.Fuse
{
    // This matches the layout of 'struct timespec' on linux-x64
    public unsafe struct TimeSpec
    {
        public long tv_sec { get; set; }
        public long tv_nsec { get; set; }

        public bool IsNow => tv_nsec == UTIME_NOW;
        public bool IsOmit => tv_nsec == UTIME_OMIT;

        public DateTime ToDateTime()
        {
            if (IsNow || IsOmit)
            {
                throw new InvalidOperationException("Cannot convert meta value to DateTime");
            }
            return new DateTime(UnixEpochTicks + TimeSpan.TicksPerSecond * tv_sec + tv_nsec / 100, DateTimeKind.Utc);
        }

        public override string ToString()
        {
            if (IsNow)
            {
                return "now";
            }
            else if (IsOmit)
            {
                return "omit";
            }
            else
            {
                return ToDateTime().ToString();
            }
        }

        public static implicit operator TimeSpec(DateTime dateTime)
        {
            dateTime = dateTime.ToUniversalTime();
            long ticks = dateTime.Ticks - UnixEpochTicks;
            long sec = ticks / TimeSpan.TicksPerSecond;
            ticks -= TimeSpan.TicksPerSecond * sec;
            long nsec = ticks * 100;
            return new TimeSpec { tv_sec = sec, tv_nsec = nsec };
        }

        private const long UnixEpochTicks = 621355968000000000;
        public const int UTIME_OMIT = 1073741822;
        public const int UTIME_NOW = 1073741823;
    }
}