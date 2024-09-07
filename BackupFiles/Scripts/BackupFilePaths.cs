using System.Collections.Concurrent;
using System.IO.Compression;
using Util;

namespace BackUp;

public class BackupFilePaths{
    private DataPath[] _fileList;
    private string[] _priorBackups;
    private string _folderPath;
    private bool _checkPriorBackups;
    private FilePathData _filePathTotal;

    private ConcurrentDictionary<string,string> _filePathsToCopy = new(4,256);
    // key = path on device, Value = path in backup
    private FileData[] _priorFileData;
    // key = path on device, Value = path in backup
    public BackupFilePaths(DataPath[] fileList, string[] priorBackups, string folderPath, bool checkPriorBackups){
        this._fileList = fileList;
        this._folderPath = folderPath;
        this._checkPriorBackups = checkPriorBackups;
        this._priorBackups = priorBackups;
        this._filePathTotal = new FilePathData();
        _priorFileData = [];
    }
    /// <summary>
    /// Build list of Files to be copied then backup files to drive specific zip files
    /// </summary>
    public void Run(){
        if(_fileList.Length < 1){
            return;
        }
        _filePathsToCopy.Clear();
        ConcurrentQueue<string> logQueue = new();
        string msg;
        long startTime;
        uint fileCalls = 0;
        
        //Get list of prior files
        if(_checkPriorBackups){
            startTime = DateTime.Now.Ticks;
            fileCalls += GetPriorFileData(logQueue);
            msg = string.Format("Info: Finished geting list of prior files: {0:F2}ms",(DateTime.Now.Ticks - startTime) / 10000f);
            logQueue.Enqueue(msg);
            Logs.WriteLog(logQueue.ToArray());
            logQueue.Clear();
        }

        //Building file to be copied to backup
        startTime = DateTime.Now.Ticks;
        fileCalls += GetFilesToBeCopied(logQueue);
        msg = string.Format("Info: Finished building file to copy list in: {0:F2}ms",(DateTime.Now.Ticks - startTime) / 10000f);
        logQueue.Enqueue(msg);
        Logs.WriteLog(logQueue.ToArray());
        logQueue.Clear();
        if(!NewBackup.CheckEnoughDriveSpace(_folderPath, _filePathTotal.Size)){
            Logs.WriteLog(logQueue.ToArray());
            logQueue.Clear();
            return; // return if not enough disk space
        }

        //ask user if size is ok
        if(!NewBackup.GetUserConfirmation(_filePathTotal.Size)){
            Console.WriteLine("User didn't continue with backup progress");
            Logs.WriteLog(logQueue.ToArray());
            logQueue.Clear();
            return; //close if "n"
        }

        //Building the backup
        Console.WriteLine("Backup Started");
        startTime = DateTime.Now.Ticks;

        //Create directory tree
        NewBackup.CreateDirectoryTreeDown(_folderPath);
        msg = string.Format("Info: File Tree Built in: {0:F2}ms",(DateTime.Now.Ticks - startTime) / 10000f);
        logQueue.Enqueue(msg);
        Console.WriteLine("File Tree Built");

        //Copy Files to drive specific zip files
        uint copiesFailed = 0;
        startTime = DateTime.Now.Ticks;
        CopyFilesToDriveSpecificZips(logQueue);


        msg = string.Format("Info: Finished coping files in: {0:F2}ms",(DateTime.Now.Ticks - startTime) / 10000f);
        logQueue.Enqueue(msg);
        if(copiesFailed > 0){
            Console.WriteLine("Backup partly completed at: " + _folderPath + "\nWith " + copiesFailed + " files failed to copy.\nTo see all files that failed to copy check the log");
            logQueue.Enqueue("Warning: Files failed to copy: " + copiesFailed);
        }else{
            Console.WriteLine("Backup Completed at: " + _folderPath);
        }
        Logs.WriteLog(logQueue.ToArray());
        logQueue.Clear();   
    }
    //Get file Metadata from old backups
    private uint GetPriorFileData(ConcurrentQueue<string> logQueue){
        DirectoryInfo[]? directoryInfo = null;
        try{
            directoryInfo = new DirectoryInfo[_priorBackups.Length];
            for (int i = 0; i < _priorBackups.Length; i++){
                directoryInfo[i] = new DirectoryInfo(_priorBackups[i]);
            }
        }catch(Exception e){
            string msg = "Error: Getting prior backup info: " + e.Message + " \nTrace: " + e.StackTrace;
            logQueue.Enqueue(msg);
        }
        if(directoryInfo == null){
            string msg = "Error: DirectoryInfo was null on GetPriorFileData";
            logQueue.Enqueue(msg);
            return 0;
        }

        ConcurrentBag<FileData> fileData = new();
        Thread[] threads = new Thread[_priorBackups.Length];
        uint fileCalls = 0;
        int k = 0;
        
        try{
            foreach (var dir in directoryInfo){
                int filesNumber = dir.GetFiles().Length;
                if(filesNumber != 0 && dir.GetDirectories().Length != 0){
                    string msg = "Error: Has extra files or directories in " + dir.FullName;
                    logQueue.Enqueue(msg);
                    Console.WriteLine(msg);
                    continue;
                }
                if(filesNumber == 0){
                    //old backup method
                    threads[k] = new Thread(() => GetPriorFileDataFromDirectories(dir, fileData, logQueue));
                    if(threads[k] != null){
                    threads[k].Start();
                    }
                }else{
                    //new backup method
                    threads[k] = new Thread(() => GetPriorFileDataFromZip(dir, fileData, logQueue));
                    if(threads[k] != null){
                    threads[k].Start();
                    }
                }
                k++;
            }
        }catch(Exception e){
            string msg = "Error: Getting prior backup info: " + e.Message + " \nTrace: " + e.StackTrace;
            logQueue.Enqueue(msg);
        }
        //Join threads
        for (int i = 0; i < _priorBackups.Length; i++){
            if(threads[i] != null){
                threads[i].Join();
            }
#pragma warning disable CS8625
            threads[i] = null; //remove ref to it
#pragma warning restore CS8625
        }

        //Sort Data
        FileData[] unCompressed = fileData.ToArray();
        Array.Sort(unCompressed);
        fileCalls = (uint)unCompressed.Length;

        //Remove older Duplicates from Data
        FileData data = unCompressed[0];
        FileData temp;
        int j = 0;
        for (int i = 1; i < unCompressed.Length; i++){
            temp = unCompressed[i];
            if(data.CompareTo(temp) == 0){
                if(data.DateTimeSet > temp.DateTimeSet){
                    continue;
                }
            }else{
                j++;
            }
            unCompressed[j] = temp;
            data = temp;
        }
        _priorFileData = new FileData[j+1];
        for (int i = 0; i < j+1; i++){
            _priorFileData[i] = unCompressed[i];
        }
        GC.Collect();
        return fileCalls;
    }

    private void GetPriorFileDataFromDirectories(DirectoryInfo directory, ConcurrentBag<FileData> fileData, ConcurrentQueue<string> logQueue){
        try{
            Stack<DirectoryInfo> directories = new Stack<DirectoryInfo>();
            directories.Push(directory);
            DirectoryInfo temp;
            int dirPath = directory.FullName.Length;
            while(directories.Count > 0){
                temp = directories.Pop();
                foreach(var dir2 in temp.GetDirectories()){
                    directories.Push(dir2);
                }
                foreach(var file in temp.GetFiles()){
                    //Rebuild drive file path
                    string fileFullName = file.FullName[dirPath] + ":" + file.FullName[(dirPath+1)..];
                    fileData.Add(new(fileFullName, file.Length, file.LastWriteTime));
                }
            }
        }catch(Exception e){
            string msg = "Error: Getting prior backup info: " + e.Message + " \nTrace: " + e.StackTrace;
            logQueue.Enqueue(msg);
        }
    }

    private void GetPriorFileDataFromZip(DirectoryInfo directory, ConcurrentBag<FileData> fileData, ConcurrentQueue<string> logQueue){
        try{
            foreach (var file in directory.GetFiles()){
                if(file.Name.Length != 5 || file.Extension != ".zip"){
                    string msg = "Error: Has extra files or directories in " + directory.FullName;
                    logQueue.Enqueue(msg);
                    Console.WriteLine(msg);
                    continue;
                }
                string drive = file.Name[0] + ":";
                ZipArchive archive = ZipFile.OpenRead(file.FullName);
                foreach(var entry in archive.Entries){
                    //Rebuild drive file path
                    if(entry.Name.Length == 0){
                        continue;//remove root file path
                    }
                    string fileFullName = drive + entry.FullName.Replace('/', '\\')[1..];
                    fileData.Add(new(fileFullName, entry.Length, entry.LastWriteTime.DateTime));
                }
            }
        }catch(Exception e){
            string msg = "Error: Getting prior backup info: " + e.Message + " \nTrace: " + e.StackTrace;
            logQueue.Enqueue(msg);
        }
    }

    private void CopyFilesToDriveSpecificZips(ConcurrentQueue<string> logQueue){
        throw new NotImplementedException();
    }

    private uint GetFilesToBeCopied(ConcurrentQueue<string> logQueue){
        //Already got files from prior backups


        //Get list of files to copy
        List<FilePathData> filePathData = new List<FilePathData>(4);
        List<Thread> threads = new List<Thread>();
        int driveIndex = 0;
        int startIndex = 0;
        filePathData.Add(new FilePathData(0,0, _fileList[driveIndex].drive));
        for (int i = 0; i < _fileList.Length; i++){
            if(_fileList[i].drive != filePathData[driveIndex].DriveLetter){
                filePathData.Add(new FilePathData(0,0, _fileList[i].drive));
                threads.Add(new Thread(() => GetFilePathsForDrive(new Span<DataPath>(_fileList, startIndex, i - startIndex), logQueue, filePathData[driveIndex])));
                threads[driveIndex].Start();
                driveIndex++;
            }
        }
        threads.Add(new Thread(() => GetFilePathsForDrive(new Span<DataPath>(_fileList, startIndex, _fileList.Length - startIndex), logQueue, filePathData[driveIndex])));
        threads[driveIndex].Start();

        foreach(Thread thread in threads){
            thread.Join();
        }
        threads.Clear();


        foreach(FilePathData filePath in filePathData){
            _filePathTotal += filePath;
        }
        Logs.WriteLog(logQueue.ToArray());
        logQueue.Clear();  
        return _filePathTotal.FileCalls;
    }
    private void GetFilePathsForDrive(Span<DataPath> dataPaths, ConcurrentQueue<string> logQueue, FilePathData data){
        foreach (DataPath dataPath in dataPaths){
            string path = dataPath.GetFullPath();
            if(dataPath.fileType == '-'){//file
                data.Size += AddFileTypePath(ref path);
            }
            else if(dataPath.fileType == 'd'){//directory tree
                if(!Directory.Exists(path)){
                    Console.WriteLine("Error: Directory doesn't exist: " + path);
                    logQueue.Enqueue("Error: Directory doesn't exist: " + path);
                    continue;
                }
                //Add files in directories to list
                CreateDirectoryTreeUp(ref path, ref data, logQueue);
            }
            else{
                Console.WriteLine("Error: Failed to handle: " + path);
                logQueue.Enqueue("Error: Failed to handle: " + path);
            }
        }
    }

    private void CreateDirectoryTreeUp(ref string path, ref FilePathData data, ConcurrentQueue<string> logQueue){
        if(path.Length > Data.MaxFileLength){ //don't allow for large file paths to be copied can prevent infinite loops
            Console.WriteLine("Error: Too long of file name: " + path);
            logQueue.Enqueue("Error: Too long of file name: " + path);
            return;
        }
        try{
            DirectoryInfo directoryInfo = new(path);
            FileInfo[] fileInfos = directoryInfo.GetFiles(); //get all files in current directory
            foreach (FileInfo file in fileInfos){
                data.FileCalls++;
                if(IsFileInPriorBackups(file)){
                    continue;
                }
                if(_filePathsToCopy.TryAdd(file.FullName, path[0] + path[2..] + file.Name)){
                    data.Size += (ulong)file.Length;
                }
                //else Already exists
            }
            DirectoryInfo[] directories = directoryInfo.GetDirectories(); // get subdirectories
            foreach (DirectoryInfo dir in directories){
                string dirPath = dir.FullName + '\\';
                if(Directory.Exists(dirPath)){
                    CreateDirectoryTreeUp(ref dirPath, ref data, logQueue);
                }
            }
        }catch(Exception e){
            Console.WriteLine("Error: " + path + " \nReason: " + e.Message);
            logQueue.Enqueue("Error: " + path + " \nReason: " + e.Message);
        }
    }
    //Binary Search prior backups
    private bool IsFileInPriorBackups(FileInfo file){
        int l = 0;
        int r = _priorFileData.Length -1;
        int m = 0;
        FileData fileData = new(file.FullName, file.Length, file.LastWriteTime.ToUniversalTime());
#if DEBUG
        FileData temp;//Debug
#endif
        bool found = false;
        while(l <= r && !found){
            m = (r+l)/2;
#if DEBUG
            temp = _priorFileData[m];
#endif
            int result = fileData.CompareTo(_priorFileData[m]);
            switch(result){
                case < 0:
                    r = m-1;
                break;
                case > 0:
                    l = m+1;
                break;
                default:
                    found = true;
                break;
            }
        }
        if(found){
            if(fileData.Size == _priorFileData[m].Size){
                //Daylightsavings bug mabye... where .zip backups files are offset from by an hour from file explore aka + 1hour
                if(fileData.DateTimeSet.AddMinutes(70) > _priorFileData[m].DateTimeSet && _priorFileData[m].DateTimeSet > fileData.DateTimeSet.AddMinutes(-70)){
                    return true;
                }
            }
        }
        return false;
    }

    private static ulong AddFileTypePath(ref string path){
        throw new NotImplementedException();
    }
}