


namespace BackUp;
//File Metadata
public readonly struct FileData : IComparable<FileData>{
    public readonly string FullPath;
    public readonly long Size;
    public readonly DateTimeOffset DateTimeSet;
    public FileData(string fullPath, long size, DateTimeOffset dateTimeSet){
        FullPath = fullPath;
        Size = size;
        DateTimeSet = dateTimeSet.ToUniversalTime();
    }

    public int CompareTo(FileData other){
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