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
            ulong backupSize = 0;
            //LoadBackup
            foreach (DataPath dataPath in fileList)
            {
                string path = dataPath.GetFullPath();
                if(dataPath.fileType == '-'){//file
                    AddFileTypePath(path);
                }
                else if(dataPath.fileType == 'd'){//directory tree
                    if(!Directory.Exists(path)){
                        Utils.PrintAndLog("Error: directory doesn't exist: " + path);
                        continue;
                    }
                    //TODO add files in directories to list
                    backupSize += CreateDirectoryTreeUp(path);
                }
                else{
                    Utils.PrintAndLog("Error: failed to handle: " + path);
                }
            }
            if(!CheckEnoughDriveSpace(folderPath, backupSize)){
                return;
            }
            //ask user if size is ok
            if(!GetUserConfirmation(backupSize)){
                Console.WriteLine("User didn't continue with backup progress");
                return; //close if "n" or not enough disk space
            }
            //Build the backup
            long startTime = DateTime.Now.Ticks;
            //Create directory tree
            CreateDirectoryTreeDown(folderPath);
            foreach(var dirPath in directoryPaths){
                CreateDirectoryTreeDown(folderPath + dirPath.Key);
            }
            Console.WriteLine("File Tree Built");
            //Copy Files
            Parallel.ForEach(filePaths,file => {
                string device = file.Key;
                string backup = folderPath + file.Value;
                CopyFile(device,backup);
            });
            Logs.WriteLog(string.Format("Info: Finished in: {0:F2}ms",(DateTime.Now.Ticks - startTime) / 10000f));
            Console.WriteLine("Backup Complete at: " + folderPath);
        }
        private static void CopyFile(string device, string backup){
            if(File.Exists(backup)){
                Logs.WriteLog("Warning: File already exists " + backup);
            }else{
                try{
                    File.Copy(device,backup);
                }catch(Exception e){
                    Utils.PrintAndLog("Error: Copying file to backup failed: " + device + " \nReason: " + e);
                }
            }
        }
        private static bool CheckEnoughDriveSpace(string location, ulong backupSize){
            try{
                DriveInfo driveInfo = new("" + location[0]);
                if((ulong)driveInfo.AvailableFreeSpace - 8_000_000 < backupSize){ //require atleast 8 MB of free space left for any unforeseen issues
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
                if(priorFile.LastWriteTime != file.LastWriteTime){
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
        private ulong CreateDirectoryTreeUp(string sourcePath){
            if(sourcePath.Length > Data.MaxFileSize){ //don't allow for large file paths to be copied can prevent infinate loops
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
                        size += CreateDirectoryTreeUp(dirPath);
                    }
                }
            }catch(Exception e){
                Utils.PrintAndLog("Error: " + sourcePath + " \nReason: " + e.Message);
            }
            return size;
        }
    }
}