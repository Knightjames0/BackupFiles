using System.Collections.Concurrent;
using Util;

namespace BackUp{
    public class NewBackup{
        private List<DataPath> fileList;
        private List<string> priorBackups;
        private string folderPath;
        private bool checkPriorBackups;
        private ConcurrentDictionary<string,string> filePaths = new(32,256);
        // key = path on device, Value = path in backup
        private Dictionary<string, byte> directoryPaths = new(64);
        // Value = path in backup

        private const ulong MinimumFreeSpaceLeft = 16_000_000;
        public NewBackup(List<DataPath> fileList, List<string> priorBackups, string folderPath, bool checkPriorBackups)
        {
            this.fileList = fileList;
            this.priorBackups = priorBackups;
            this.folderPath = folderPath;
            this.checkPriorBackups = checkPriorBackups;
            BackupCreate();
        }
        
        private ulong AddFileTypePath(string path){
            if(!File.Exists(path)){
                Utils.PrintAndLog("Error: file doesn't exist: " + path);
                return 0;
            }
            FileInfo file;
            try{
                file = new FileInfo(path);
            }catch(Exception e){
                Utils.PrintAndLog("Error: " + path + " \nReason: " + e.Message);
                return 0;
            }
            if(checkPriorBackups){
                if(IsFileInPriorBackups(file)){
                    return 0;
                }
            }
            if(!filePaths.TryAdd(path,path[0] + path[2..])){
                Logs.WriteLog("Warning: may already exist in list of files to backup: " + path);
                return 0;
            }
            int t = path.LastIndexOf('\\',path.Length - 1);
            directoryPaths.TryAdd(path[0] + path[2..(t+1)],0x00);
            try{
                return (ulong) file.Length;
            }catch(Exception e){
                Utils.PrintAndLog("Error: " + path + " \nReason: " + e.Message);
                return 0;
            }
        }
        /// <summary>
        /// Create a new backup of the from the paths in fileList
        /// </summary>
        private void BackupCreate(){
            if(fileList.Count == 0){
                Console.WriteLine("No files to backup added");
                return;
            }
            string msg;
            ulong backupSize = 0;
            int fileCalls = 0;
            long startTime;
            //LoadBackup
            ConcurrentQueue<string> logQueue = new();

            //Building file to copy list
            startTime = DateTime.Now.Ticks;
            foreach (DataPath dataPath in fileList)
            {
                string path = dataPath.GetFullPath();
                if(dataPath.fileType == '-'){//file
                    backupSize += AddFileTypePath(path);
                }
                else if(dataPath.fileType == 'd'){//directory tree
                    if(!Directory.Exists(path)){
                        Utils.PrintAndLog("Error: Directory doesn't exist: " + path);
                        continue;
                    }
                    //Add files in directories to list
                    backupSize += CreateDirectoryTreeUp(path, ref fileCalls);
                }
                else{
                    Utils.PrintAndLog("Error: Failed to handle: " + path);
                }
            }
            msg = string.Format("Info: Finished building file to copy list in: {0:F2}ms",(DateTime.Now.Ticks - startTime) / 10000f);
            logQueue.Enqueue(msg);
            logQueue.Enqueue("fileCalls: " + fileCalls);
            if(!CheckEnoughDriveSpace(folderPath, backupSize)){
                Logs.WriteLog(logQueue.ToArray());
                logQueue.Clear();
                return;
            }
            //ask user if size is ok
            if(!GetUserConfirmation(backupSize)){
                Console.WriteLine("Info: User didn't continue with backup progress");
                Logs.WriteLog(logQueue.ToArray());
                logQueue.Clear();
                return; //close if "n" or not enough disk space
            }

            //Building the backup
            startTime = DateTime.Now.Ticks;
            //Create directory tree
            CreateDirectoryTreeDown(folderPath);
            foreach(var dirPath in directoryPaths){
                CreateDirectoryTreeDown(folderPath + dirPath.Key);
            }
            msg = string.Format("Info: File Tree Built in: {0:F2}ms",(DateTime.Now.Ticks - startTime) / 10000f);
            logQueue.Enqueue(msg);
            Console.WriteLine("File Tree Built");
            //Copy Files
            long copiesFailed = 0;
            startTime = DateTime.Now.Ticks;

            Parallel.ForEach(filePaths,file => {
                string device = file.Key;
                string backup = folderPath + file.Value;
                if(!CopyFile(device,backup, logQueue)){
                    Interlocked.Add(ref copiesFailed,1);//add to files failed to copy
                }
            });

            msg = string.Format("Info: Finished coping files in: {0:F2}ms",(DateTime.Now.Ticks - startTime) / 10000f);
            logQueue.Enqueue(msg);
            if(copiesFailed > 0){
                Console.WriteLine("Backup partly completed at: " + folderPath + "\nWith " + copiesFailed + " files failed to copy.\nTo see all files that failed to copy check the log");
                logQueue.Enqueue("Warning: Files failed to copy: " + copiesFailed);
            }else{
                Console.WriteLine("Backup Completed at: " + folderPath);
            }
            Logs.WriteLog(logQueue.ToArray());
            logQueue.Clear();    
        }
        /// <summary>
        /// Copies files from one location to another for parallel execution with error handling
        /// </summary>
        /// <param name="device">Source Path</param>
        /// <param name="backup">Destination Path</param>
        /// <param name="logQueue">A localtion to queue up error messages</param>
        /// <returns>Returns true if it was successful</returns>
        private static bool CopyFile(string device, string backup, ConcurrentQueue<string> logQueue){
            if(File.Exists(backup)){
                string msg = "Warning: File already exists " + backup;
                logQueue.Enqueue(msg);
                return false;
                //Logs.WriteLog("Warning: File already exists " + backup);
            }else{
                try{
                    File.Copy(device,backup);
                }catch(Exception e){
                    string msg = "Error: Copying file to backup failed: " + device + " \nReason: " + e;
                    logQueue.Enqueue(msg);
                    return false;
                    //Utils.PrintAndLog(msg);
                }
            }
            return true;
        }
        private static bool CheckEnoughDriveSpace(string location, ulong backupSize){
            try{
                DriveInfo driveInfo = new("" + location[0]);
                if((ulong)driveInfo.AvailableFreeSpace - MinimumFreeSpaceLeft < backupSize){ //require atleast 16 MB of free space left for any unforeseen issues
                    Utils.PrintAndLog("Error: Not enough free space on drive: " + location);
                    return false;
                }
                return true;
            }catch(Exception e){
                Utils.PrintAndLog("Error: Getting drive info: " + location + " \nReason: " + e);
                return false;
            }
        }
        private static bool GetUserConfirmation(ulong backupSize){
            Console.Write("The file size to be backed up is: {0:N3} Megabytes would you like to continue? (y/n) ", backupSize / 1_000_000f);
            string answer = "";
            while(answer != "y" && answer != "n"){
                Console.Write(">");
                answer = (Console.ReadLine() + "").Trim().ToLower();
                if(answer != "y" && answer != "n"){
                    Console.WriteLine("Please answer with 'y' or 'n'");
                }
            }
            return answer == "y";
        }
        /// <summary>
        /// checks if it already exist in a prior backup.
        /// also with return false is checkPriorBackups is false
        /// </summary>
        /// <param name="file">file to be checked agansit prior backups</param>
        /// <returns>returns true if it exists in prior backups</returns>
        private bool IsFileInPriorBackups(FileInfo file){
            if(!checkPriorBackups){
                return false;
            }
            string filePath = file.FullName;
            filePath = filePath[0] + filePath[2..];
            foreach(string priorBackup in priorBackups){
                string priorFilePath = priorBackup + filePath;
                if(!File.Exists(priorFilePath)){
                    continue;
                }
                FileInfo priorFile = new(priorFilePath);
                //To check if the file has been modified since prior backups
                if(priorFile.LastWriteTime.TimeOfDay != file.LastWriteTime.TimeOfDay && priorFile.LastWriteTime.Date != file.LastWriteTime.Date){
                    continue;
                }
                if(priorFile.Length != file.Length){
                    continue;
                }
                return true;
            }
            return false;
        }
        private static bool CreateDirectoryTreeDown(string path){ //should not be called out side of CreateDirectoryTree
            if(!Directory.Exists(path)){
                short t = (short)path.LastIndexOf('\\',path.Length - 2);
                if(t == -1){
                    return CreateDir(path); 
                }
                if(t > 0){
                    if(CreateDirectoryTreeDown(path[..(t+1)])){
                        return CreateDir(path); 
                    }
                }
            }else{
                return true;
            }
            return false;
        }
        /// <summary>
        /// Creates a directory at a specified path
        /// </summary>
        /// <param name="path">path to create directory at</param>
        /// <returns>returns true if it was successful </returns>
        private static bool CreateDir(string path){
            try{
                Directory.CreateDirectory(path);
                return true;
            }catch(Exception e){
                Logs.WriteLog("Error: Creating directory: " + path + " - " + e.Message);
                return false;
            }
        }
        /// <summary>
        /// adds all files to filePaths and all directories to directoryPaths
        /// </summary>
        /// <param name="sourcePath">staring point</param>
        /// <returns>the size of all the files part of this file tree that aren't already counted</returns>
        private ulong CreateDirectoryTreeUp(string sourcePath, ref int fileCalls){
            if(sourcePath.Length > Data.MaxFileLength){ //don't allow for large file paths to be copied can prevent infinite loops
                Utils.PrintAndLog("Error: Too long of file name: " + sourcePath);
                return 0;
            }
            if(!directoryPaths.TryAdd(sourcePath[0] + sourcePath[2..],0x00)){
                //Already exists
            }
            ulong size = 0;
            try{
                DirectoryInfo directoryInfo = new(sourcePath);
                FileInfo[] fileInfos = directoryInfo.GetFiles(); //get all files in current directory
                foreach (FileInfo file in fileInfos){
                    fileCalls++;
                    if(IsFileInPriorBackups(file)){
                        continue;
                    }
                    if(filePaths.TryAdd(file.FullName, sourcePath[0] + sourcePath[2..] + file.Name)){
                        size += (ulong)file.Length;
                    }
                    //else Already exists
                }
                DirectoryInfo[] directories = directoryInfo.GetDirectories(); // get subdirectories
                foreach (DirectoryInfo dir in directories){
                    string dirPath = dir.FullName + '\\';
                    if(Directory.Exists(dirPath)){
                        size += CreateDirectoryTreeUp(dirPath, ref fileCalls);
                    }
                }
            }catch(Exception e){
                Utils.PrintAndLog("Error: " + sourcePath + " \nReason: " + e.Message);
            }
            return size;
        }
    }
}