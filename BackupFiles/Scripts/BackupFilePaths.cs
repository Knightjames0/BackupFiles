using System.Collections.Concurrent;
using System.IO.Compression;
using Util;

namespace BackUp;

public class BackupFilePaths{
    private DataPath[] _fileList;
    private string[] _priorBackups;
    private string _folderPath;
    private bool _checkPriorBackups;
    private bool _backupTozip;
    private FilePathData _filePathTotal;
    public readonly long TimeAllowOffset = new DateTime(0).AddMinutes(70).Ticks;
    private ConcurrentDictionary<string,string> _filePathsToCopy = new(4,256);
    // key = path on device, Value = path in backup
    private MetaData[] _priorMetaData;
    // key = path on device
    public BackupFilePaths(DataPath[] fileList, string[] priorBackups, string folderPath, bool checkPriorBackups, bool backupToZip){
        this._fileList = fileList;
        this._folderPath = folderPath;
        this._checkPriorBackups = checkPriorBackups;
        this._priorBackups = priorBackups;
        this._backupTozip = backupToZip;
        this._filePathTotal = new FilePathData();
        _priorMetaData = [];
    }
    /// <summary>
    /// Build list of Files to be copied then backup files to drive specific zip files
    /// </summary>
    public void Run(){
        if(_fileList.Length < 1){
            return;
        }
        Utils.QuickSort(_fileList);
        _filePathsToCopy.Clear();
        ConcurrentQueue<string> logQueue = new();
        string msg;
        long startTime;
        _filePathTotal = new FilePathData();
        
        //Get list of prior files
        if(_checkPriorBackups){
            startTime = DateTime.Now.Ticks;
            _priorMetaData = GetPriorBackupsFileMetaData(logQueue, _priorBackups);
            msg = string.Format("Info: Finished getting list of prior files: {0:F2}ms",(DateTime.Now.Ticks - startTime) / 10000f);
            logQueue.Enqueue(msg);
        }

        //Building file to be copied to backup
        startTime = DateTime.Now.Ticks;
        char[] driveLetters = GetFilesToBeCopied(logQueue);
        logQueue.Enqueue("Info: fileCalls: " + _filePathTotal.FileCalls);
        msg = string.Format("Info: Finished building files to copy list in: {0:F2}ms",(DateTime.Now.Ticks - startTime) / 10000f);
        logQueue.Enqueue(msg);
        
        Logs.WriteLog(logQueue.ToArray());
        logQueue.Clear();

        if(!NewBackup.CheckEnoughDriveSpace(_folderPath, _filePathTotal.Size)){
            Logs.WriteLog(logQueue.ToArray());
            logQueue.Clear();
            return; // return if not enough disk space
        }

        //ask user if size is ok
        if(!NewBackup.GetUserConfirmation(string.Format("The file size to be backed up is: {0:N3} Megabytes would you like to continue?", _filePathTotal.Size / 1_000_000f))){
            Console.WriteLine("User didn't continue with backup progress");
            Logs.WriteLog(logQueue.ToArray());
            logQueue.Clear();
            return; //close if "n"
        }

        //Building the backup
        Console.WriteLine("Backup Started");
        startTime = DateTime.Now.Ticks;

        //Create directory down tree
        if(!NewBackup.CreateDirectoryTreeDown(_folderPath)){
            msg = "Error: Failed to build Base File Tree";
            logQueue.Enqueue(msg);
            Console.WriteLine(msg);
            Logs.WriteLog(logQueue.ToArray());
            logQueue.Clear();
            return;
        }
        if(!_backupTozip){
            //Regular File Tree
            if(!CreateDirectoryFromDataPaths(_folderPath, _fileList)){
                msg = "Error: Failed to create directory tree";
                logQueue.Enqueue(msg);
                Console.WriteLine(msg);
                Logs.WriteLog(logQueue.ToArray());
                logQueue.Clear();
                return;
            }
        }
        msg = string.Format("Info: File Tree Built in: {0:F2}ms",(DateTime.Now.Ticks - startTime) / 10000f);
        logQueue.Enqueue(msg);
        Console.WriteLine("File Tree Built");
        startTime = DateTime.Now.Ticks;
        //Copy Files to drive specific zip files
        ulong unCompressSize = 0;
        if(_backupTozip){
            //Zip
            unCompressSize = CopyFilesToDriveSpecificZips(driveLetters, logQueue);
        }else{
            //Regular File Tree
            unCompressSize = CopyFilesToFolders(logQueue);
        }


        msg = string.Format("Info: Finished coping files in: {0:F2}ms",(DateTime.Now.Ticks - startTime) / 10000f);
        logQueue.Enqueue(msg);
        if(unCompressSize != _filePathTotal.Size){
            msg = string.Format("Error: Backup failed with ({0}bytes / {1}bytes) being copied successfully", unCompressSize, _filePathTotal.Size);
            Console.WriteLine(msg);
            logQueue.Enqueue(msg);
        }else{
            Console.WriteLine("Backup Completed at: " + _folderPath);
        }
        Logs.WriteLog(logQueue.ToArray());
        logQueue.Clear();   
    }

    //Get file Metadata from old backups
    private MetaData[] GetPriorBackupsFileMetaData(ConcurrentQueue<string> logQueue, string[] priorBackupPaths){
        DirectoryInfo[] directoryInfo = new DirectoryInfo[_priorBackups.Length];
        try{
            for (int i = 0; i < _priorBackups.Length; i++){
                directoryInfo[i] = new DirectoryInfo(_priorBackups[i]);
            }
        }catch(Exception e){
            string msg = "Error: Getting prior File MetaData from backup.\nMessage: " + e.Message + " \nTrace: " + e.StackTrace;
            logQueue.Enqueue(msg);
        }
        if(directoryInfo == null){
            string msg = "Error: DirectoryInfo was null on GetPriorFileData";
            logQueue.Enqueue(msg);
            return MetaData.Empty;
        }
        if(directoryInfo.Length == 0){
            string msg = "Error: DirectoryInfo was empty on GetPriorFileData";
            logQueue.Enqueue(msg);
            return MetaData.Empty;
        }

        ConcurrentBag<MetaData> fileData = new();
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
                    //directory paths backup method
                    threads[k] = new Thread(() => GetPriorFileDataFromDirectories(dir, fileData, logQueue));
                    if(threads[k] != null){
                        threads[k].Start();
                    }
                }else{
                    //zip compression backup method
                    threads[k] = new Thread(() => GetPriorFileDataFromZip(dir, fileData, logQueue));
                    if(threads[k] != null){
                        threads[k].Start();
                    }
                }
                k++;
            }
        }catch(Exception e){
            string msg = "Error: GetPriorFileData with Threading.\nMessage: " + e.Message + " \nTrace: " + e.StackTrace;
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
        MetaData[] allPriorMetaData = fileData.ToArray();
        Array.Sort(allPriorMetaData);
        fileCalls = (uint)allPriorMetaData.Length;
        _filePathTotal.FileCalls += fileCalls;

        //Remove older Duplicate MetaData and by newest MetaData
        int j = 0;
        for (int i = 1; i < allPriorMetaData.Length; i++){
            if(allPriorMetaData[j].CompareTo(allPriorMetaData[i]) == 0){
                if(allPriorMetaData[j].DateTime > allPriorMetaData[i].DateTime){
                    continue;
                }
            }else{
                j++;
            }
            allPriorMetaData[j] = allPriorMetaData[i];
        }
        //Copy to a new array the output
        MetaData[] priorMetaData = new MetaData[j+1];
        for (int i = 0; i < j+1; i++){
            priorMetaData[i] = allPriorMetaData[i];
        }
        GC.Collect();
        return priorMetaData;
    }
    /// <summary>Directory paths backup method</summary>
    private static void GetPriorFileDataFromDirectories(DirectoryInfo baseDirectory, ConcurrentBag<MetaData> fileData, ConcurrentQueue<string> logQueue){
        try{
            Stack<DirectoryInfo> directories = new Stack<DirectoryInfo>();
            foreach (var directory in baseDirectory.GetDirectories()){
                if(directory.Name.Length != 1){
                    logQueue.Enqueue("Error: Unexpected directory in prior backup: " + baseDirectory.FullName);
                    continue;
                }
                directories.Push(directory);
            }
            DirectoryInfo temp;
            int dirPath = baseDirectory.FullName.Length + 1;
            string drive; 
            while(directories.Count > 0){
                temp = directories.Pop();
                foreach(var dir2 in temp.GetDirectories()){
                    directories.Push(dir2);
                }
                drive = temp.FullName[dirPath-1] + ":"; //For getting the drive letter from the file path
                foreach(var file in temp.GetFiles()){
                    //Rebuild drive file path
                    string fileFullName = drive + file.FullName[dirPath..];
                    fileData.Add(new(fileFullName, file.Length, file.LastWriteTime));
                }
            }
        }catch(Exception e){
            string msg = "Error: Getting Files in Directories in prior backup\nMessage: " + e.Message + " \nTrace: " + e.StackTrace;
            logQueue.Enqueue(msg);
        }
    }
    /// <summary>Zip compression backup method</summary>
    private static void GetPriorFileDataFromZip(DirectoryInfo directory, ConcurrentBag<MetaData> fileData, ConcurrentQueue<string> logQueue){
#if DEBUG
        string fileName = "";
#endif
        try{
            foreach (var file in directory.GetFiles()){
                if(file.Name.Length != 5 || file.Extension != ".zip"){
                    string msg = "Error: Has extra files or directories in " + directory.FullName;
                    logQueue.Enqueue(msg);
                    Console.WriteLine(msg);
                    continue;
                }
                string drive = file.Name[0] + ":\\";
#if DEBUG
                fileName = file.FullName;
#endif
                using ZipArchive archive = ZipFile.OpenRead(file.FullName);
                foreach(var entry in archive.Entries){
                    //Rebuild drive file path
                    if(entry.Name.Length == 0){
                        continue;//remove root file path
                    }
                    string fileFullName = drive + entry.FullName;
                    fileData.Add(new(fileFullName, entry.Length, entry.LastWriteTime.DateTime));
                }
            }
        }catch(Exception e){
#if DEBUG
            string msg = "Error: Copying files from prior zip backup " + fileName + ".\nMessage: " + e.Message + " \nTrace: " + e.StackTrace;
#else   
            string msg = "Error: Copying files from prior zip backup.\nMessage: " + e.Message + " \nTrace: " + e.StackTrace;
#endif
            logQueue.Enqueue(msg);
        }
    }
    /// <summary>
    /// Copies all files in _filePathsToCopy and creates the zip files on a per drive basis
    /// </summary>
    private ulong CopyFilesToDriveSpecificZips(char[] driveLetters, ConcurrentQueue<string> logQueue){
        
        //throw new NotImplementedException();
        //Create all base zipFiles

        string[] filePathsToCopies = new string[1];
        DriveCopyData[] driveCopyData = new DriveCopyData[driveLetters.Length];
        Thread[] threads = new Thread[driveLetters.Length];
        for(int i = 0; i < driveLetters.Length; i++){
            driveCopyData[i] = new DriveCopyData(driveLetters[i]);
            int index = i;
            Thread thread = new Thread(() => CopyFilesToZip(ref driveCopyData, index, logQueue));
            threads[i] = thread;
            threads[i].Start();
        }

        for(int i = 0; i < driveLetters.Length; i++){
            threads[i].Join();
#pragma warning disable CS8625
            threads[i] = null; //remove ref to it
#pragma warning restore CS8625
        }
        
        ulong filesFailedToCopy = 0;
        foreach(DriveCopyData driveData in driveCopyData){
            filesFailedToCopy += (ulong) driveData.UnCompressSize;
        }
        return filesFailedToCopy;
    }
    private void CopyFilesToZip(ref DriveCopyData[] data, int index, ConcurrentQueue<string> logQueue){
        //Thread
        string zipFile = _folderPath + data[index].DriveLetter + ".zip";
        try{
            //Write files to zip file
            ZipArchive archive = ZipFile.Open(zipFile, ZipArchiveMode.Create);
            foreach (var fileToCopy in _filePathsToCopy){
                if (fileToCopy.Key[0] != data[index].DriveLetter){
                    continue;
                }
                archive.CreateEntryFromFile(fileToCopy.Key, fileToCopy.Value);
                //data[index].UnCompressSize = (ulong)temp.Length;
            }
            archive.Dispose();
            //Valid all files where written to zip file
            archive = ZipFile.OpenRead(zipFile);
            foreach( var entry in archive.Entries){
                data[index].UnCompressSize += entry.Length;
            }
            archive.Dispose();
        }
        catch(Exception e){
            string msg = "Error: Copying files to new zip backup.\nMessage: " + e.Message + " \nTrace: " + e.StackTrace;
            logQueue.Enqueue(msg);
        }
    }

    private char[] GetFilesToBeCopied(ConcurrentQueue<string> logQueue){
        //Already got files from prior backups


        //Get list of files to copy
        List<FilePathDataT> filePathDataTemp = new List<FilePathDataT>(4);
        List<Thread> threads = new List<Thread>();
        int startIndex = 0;
        char c = _fileList[0].Drive;
        for (int i = 0; i < _fileList.Length; i++){
            if(_fileList[i].Drive != c){
                filePathDataTemp.Add(new FilePathDataT(startIndex, i - startIndex, c));
                c = _fileList[i].Drive;

                startIndex = i;
            }
        }
        filePathDataTemp.Add(new FilePathDataT(startIndex, _fileList.Length - startIndex, c));

        //Rebuild filePathData for threading enviroment
        FilePathDataT[] filePathData = filePathDataTemp.ToArray();
        for (int i = 0; i < filePathData.Length; i++){
            int temp = i;
            Thread tempThreadFinal = new Thread(() => GetFilePathsForDrive(_fileList, logQueue, ref filePathData, temp));
            tempThreadFinal.Start();
            threads.Add(tempThreadFinal);
        }
        
        foreach(Thread thread in threads){
            thread.Join();
        }
        threads.Clear();
        char[] driveLetters = new char[filePathData.Length];
        for(int i = 0; i < filePathData.Length; i++){
            _filePathTotal.FileCalls += filePathData[i].FileCalls;
            _filePathTotal.Size += filePathData[i].Size;
            driveLetters[i] = filePathData[i].DriveLetter;
        }
        Logs.WriteLog(logQueue.ToArray());
        logQueue.Clear();  
        return driveLetters;
    }
    private void GetFilePathsForDrive(DataPath[] dataPaths, ConcurrentQueue<string> logQueue, ref FilePathDataT[] data, int index){
        Span<DataPath> datapathsTemp = new Span<DataPath>(dataPaths, data[index].Start, data[index].Length);
        foreach (DataPath dataPath in datapathsTemp){
            string path = dataPath.GetFullPath();
            if(dataPath.FileType == '-'){//file
                AddFileTypePath(ref path, ref data, ref index, logQueue);
            }
            else if(dataPath.FileType == 'd'){//directory tree
                if(!Directory.Exists(path)){
                    Console.WriteLine("Error: Directory doesn't exist: " + path);
                    logQueue.Enqueue("Error: Directory doesn't exist: " + path);
                    continue;
                }
                //Add files in directories to list
                GetDirectoriesTreeUp(ref path, ref data, ref index, logQueue);
            }
            else{
                Console.WriteLine("Error: Failed to handle: " + path);
                logQueue.Enqueue("Error: Failed to handle: " + path);
            }
        }
    }

    private void GetDirectoriesTreeUp(ref string path, ref FilePathDataT[] data, ref int index, ConcurrentQueue<string> logQueue){
        if(path.Length > Data.MaxFileLength){ //don't allow for large file paths to be copied can prevent infinite loops
            Console.WriteLine("Error: Too long of file name: " + path);
            logQueue.Enqueue("Error: Too long of file name: " + path);
            return;
        }
        try{
            DirectoryInfo directoryInfo = new(path);
            FileInfo[] fileInfos = directoryInfo.GetFiles(); //get all files in current directory
            foreach (FileInfo file in fileInfos){
                data[index].FileCalls++;
                if(IsFileInPriorBackups(file)){
                    continue;
                }
                if(_filePathsToCopy.TryAdd(file.FullName, path[3..] + file.Name)){
                    data[index].Size += (ulong)file.Length;
                }
                //else Already exists
            }
            DirectoryInfo[] directories = directoryInfo.GetDirectories(); // get subdirectories
            foreach (DirectoryInfo dir in directories){
                string dirPath = dir.FullName + '\\';
                if(Directory.Exists(dirPath)){
                    GetDirectoriesTreeUp(ref dirPath, ref data, ref index, logQueue);
                }
            }
        }catch(Exception e){
            Console.WriteLine("Error: " + path + " \nReason: " + e.Message);
            logQueue.Enqueue("Error: " + path + " \nReason: " + e.Message);
        }
    }
    //Binary Search prior backups
    private bool IsFileInPriorBackups(FileInfo file){
        if(!_checkPriorBackups){
            return false;
        }
        int l = 0;
        int r = _priorMetaData.Length -1;
        int m = 0;
        MetaData fileData = new(file.FullName, file.Length, file.LastWriteTime.ToUniversalTime());
#if DEBUG
        MetaData temp;
#endif
        bool found = false;
        while(l <= r && !found){
            m = (r+l)/2;
#if DEBUG
            temp = _priorMetaData[m];
#endif
            int result = fileData.CompareTo(_priorMetaData[m]);
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
            if(fileData.Size == _priorMetaData[m].Size){
                //Daylightsavings bug mabye... where .zip backups files are offset from by an hour from file explore e.g +/- 1 hour
                if(fileData.DateTime + TimeAllowOffset >= _priorMetaData[m].DateTime && _priorMetaData[m].DateTime >= fileData.DateTime - TimeAllowOffset){
                    return true;
                }
            }
        }
        return false;
    }

    private void AddFileTypePath(ref string path, ref FilePathDataT[] data, ref int index, ConcurrentQueue<string> logQueue){
        FileInfo fileInfo;
        string msg;
        if(!File.Exists(path)){
                msg = "Error: File doesn't exist: " + path;
                logQueue.Enqueue(msg);
                Console.WriteLine(msg);
                return;
            }
        try{
            fileInfo = new FileInfo(path);
        }catch(Exception e){
            msg = "Error: " + path + " \nReason: " + e.Message;
            logQueue.Enqueue(msg);
            Console.WriteLine(msg);
            return;
        }
        data[index].FileCalls++;
        if(IsFileInPriorBackups(fileInfo)){
            return;
        }
        if(_filePathsToCopy.TryAdd(fileInfo.FullName, path[3..])){
            data[index].Size += (ulong)fileInfo.Length;
        }
    }
    //regualer file coping
    private ulong CopyFilesToFolders(ConcurrentQueue<string> logQueue){
        ulong filesCopiedSize = 0;
        Parallel.ForEach(_filePathsToCopy,file => {
            string devicePath = file.Key;
            char drive = devicePath[0];
            string backupPath = _folderPath + drive + '\\' + file.Value;
            ulong val = CopyFile(devicePath, backupPath, logQueue);
            Interlocked.Add(ref filesCopiedSize, val);//add to files failed to copy
        });
        return filesCopiedSize;
    }

    //Copies a file and returns the backupPath's file size
    private ulong CopyFile(string sourcePath, string backupPath, ConcurrentQueue<string> logQueue){
        ulong size;
        string msg;
        if(File.Exists(backupPath)){
            msg = "Warning: File already exists " + backupPath;
            logQueue.Enqueue(msg);
            return 0;
        }
        try{
            File.Copy(sourcePath,backupPath);
            size = (ulong)new FileInfo(backupPath).Length;
        }catch(Exception e){
            msg = "Error: Copying file to backup failed: " + sourcePath + " \nReason: " + e;
            logQueue.Enqueue(msg);
            return 0;
        }
        return size;
    }

    //Build the directory up and down for each Datapath
    private bool CreateDirectoryFromDataPaths(string folderPath, DataPath[] fileList){
        string path;
        string sourcePath;
        foreach(DataPath dataPath in fileList){
            if(dataPath.FileType != 'd'){
                path = folderPath + dataPath.Drive + "\\" + dataPath.Path;
                int lastSlash = path.LastIndexOf("\\") + 1;
                path = path[..lastSlash];
                if(!NewBackup.CreateDirectoryTreeDown(path)){
                return false;
                }
                continue;
            }
            //Directories
            path = folderPath + dataPath.Drive + "\\" + dataPath.Path;
            sourcePath = dataPath.GetFullPath();
            Stack<DirectoryInfo> next = new();
            if(!NewBackup.CreateDirectoryTreeDown(path)){
                return false;
            }
            try{
                next.Push(new DirectoryInfo(sourcePath));
                while(next.Count > 0){
                    DirectoryInfo dirInfo = next.Pop();
                    string tempPath = folderPath + dirInfo.FullName[0] + dirInfo.FullName[2..];
                    if(tempPath.Length >= Data.MaxFileLength){
                        throw new PathTooLongException("Error: Path length was greater then max length: " + Data.MaxFileLength);
                    }
                    if(!Directory.Exists(tempPath)){
                        Directory.CreateDirectory(tempPath);
                    }
                    foreach(DirectoryInfo directoryInfo in dirInfo.GetDirectories()){
                        next.Push(directoryInfo);
                    }
                }
            }catch(Exception e){
                Utils.PrintAndLog(e.Message);
                return false;
            }
        }
        return true;  
    }
}