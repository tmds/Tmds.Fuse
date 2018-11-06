#include <sys/stat.h>
#include <stdio.h>
#include <stddef.h>
#include <time.h>
#include <string.h>

#define FUSE_USE_VERSION 31
#include <fuse3/fuse.h>


struct FileInfo
{
    int flags;
    int bitfields;
};

int GetBitFieldMask(struct fuse_file_info* fi)
{
    return ((struct FileInfo*)fi)->bitfields;
}

int main()
{
    printf("StatSizeOf = %d\n", sizeof(struct stat));
    printf("StatLongSizeOf = %d\n", (sizeof(struct stat) + 7) / 8);

    printf("StatOffsetOfStMode = %d\n", offsetof(struct stat, st_mode));
    _Static_assert(sizeof(((struct stat*)0)->st_mode) == 4, "st_mode size");

    printf("StatOffsetOfStSize = %d\n", offsetof(struct stat, st_size));
        _Static_assert(sizeof(((struct stat*)0)->st_size) == 8, "st_size size");

    printf("StatOffsetOfNLink = %d\n", offsetof(struct stat, st_nlink));
    _Static_assert(sizeof(((struct stat*)0)->st_nlink) == sizeof(void*), "st_nlink size");

    printf("StatOffsetOfStATime = %d\n", offsetof(struct stat, st_atime));
    _Static_assert(sizeof(((struct stat*)0)->st_atime) == sizeof(time_t), "st_atime size");

    printf("StatOffsetOfStMTime = %d\n", offsetof(struct stat, st_mtime));
    _Static_assert(sizeof(((struct stat*)0)->st_mtime) == sizeof(time_t), "st_mtime size");
    printf("StatOffsetOfStATimeNsec = %d\n", offsetof(struct stat, st_atim.tv_nsec));
    _Static_assert(sizeof(((struct stat*)0)->st_atim.tv_nsec) == sizeof(void*), "st_atim.tv_nsec size");
    printf("StatOffsetOfStMTimeNsec = %d\n", offsetof(struct stat, st_mtim.tv_nsec));
    _Static_assert(sizeof(((struct stat*)0)->st_mtim.tv_nsec) == sizeof(void*), "st_mtim.tv_nsec size");

    printf("TimespecSizeOf = %d\n", sizeof(struct timespec));

    printf("TimespecOffsetOfTvSec = %d\n", offsetof(struct timespec, tv_sec));
    _Static_assert(sizeof(((struct timespec*)0)->tv_sec) == sizeof(time_t), "tv_sec size");

    printf("TimespecOffsetOfTvNsec = %d\n", offsetof(struct timespec, tv_nsec));
    _Static_assert(sizeof(((struct timespec*)0)->tv_nsec) == sizeof(void*), "tv_nsec size");

    printf("TimeTSizeOf = %d\n", sizeof(time_t));
    printf("UTIME_OMIT = %d\n", UTIME_OMIT); // less or equal int32.max
    printf("UTIME_NOW = %d\n", UTIME_NOW); // less or equal int32.max

    struct fuse_file_info fi;
    memset(&fi, 0, sizeof(fi));
    fi.direct_io = 1;
    printf("FileInfoDirectIoFieldMask = %d\n", GetBitFieldMask(&fi));
    fi.direct_io = 0;
    return 0;
}