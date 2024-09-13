
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
public class FilePathData{
    public readonly char DriveLetter;
    public ulong Size;
    public uint FileCalls;
    public FilePathData(ulong fileSize = 0, uint fileCalls = 0, char driveLetter = '.') {
        Size = fileSize;
        FileCalls = fileCalls;
        DriveLetter = driveLetter;
    }
    public static FilePathData operator + (FilePathData a, FilePathData b){
        return new FilePathData(a.Size + b.Size, a.FileCalls + b.FileCalls);
    }
}
public struct FilePathDataT{
    public readonly char DriveLetter;
    public readonly int Start;
    public readonly int Length;
    public uint FileCalls;
    public ulong Size;
    public FilePathDataT(int start, int length , char driveLetter, ulong size = 0, uint fileCalls = 0) {
        Start = start;
        Length = length;
        Size = size;
        FileCalls = fileCalls;
        DriveLetter = driveLetter;
    }
    public static FilePathData operator + (FilePathDataT a, FilePathData b){
        return new FilePathData(a.Size + b.Size, a.FileCalls + b.FileCalls);
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