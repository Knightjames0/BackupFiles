
namespace BackUp;
//File Metadata
public readonly struct MetaData : IComparable<MetaData>{
    public readonly string FullPath;
    public readonly long Size;
    public readonly long DateTime;
    public MetaData(string fullPath, long size, DateTimeOffset dateTimeOffset){
        FullPath = fullPath;
        Size = size;
        DateTime = dateTimeOffset.ToUniversalTime().Ticks;
    }

    public static MetaData[] Empty { get{ return new MetaData[0]; } }

    public int CompareTo(MetaData other){
        return FullPath.CompareTo(other.FullPath);
    }
}
public struct BackupData{
    public ulong Size;
    public uint FileCalls;
    public BackupData(ulong fileSize = 0, uint fileCalls = 0) {
        Size = fileSize;
        FileCalls = fileCalls;
    }
    public static BackupData operator + (BackupData a, BackupData b){
        return new BackupData(a.Size + b.Size, a.FileCalls + b.FileCalls);
    }
}
public struct FileCopyData{
    public readonly char DriveLetter;
    public readonly int Start;
    public readonly int Length;
    public uint FileCalls;
    public ulong Size;
    public FileCopyData(int start, int length , char driveLetter, ulong size = 0, uint fileCalls = 0) {
        Start = start;
        Length = length;
        Size = size;
        FileCalls = fileCalls;
        DriveLetter = driveLetter;
    }
    public static BackupData operator + (FileCopyData a, BackupData b){
        return new BackupData(a.Size + b.Size, a.FileCalls + b.FileCalls);
    }
}
public struct DriveCopyData{
    public readonly char DriveLetter;
    public long UnCompressSize; 
    public DriveCopyData(char driveLetter, long unCompressSize = 0) {
        DriveLetter = driveLetter;
        UnCompressSize = unCompressSize;
    }
}