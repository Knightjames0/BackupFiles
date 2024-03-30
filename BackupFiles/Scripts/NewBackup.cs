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
                    if(!File.Exists(path)){
                        Utils.PrintAndLog("Error: file doesn't exist: " + path);
                        continue;
                    }
                    if(checkPriorBackups){
                        FileInfo file = new FileInfo(path);
                        if(IsFileInPriorBackups(file)){
                            continue;
                        }
                    }
                    if(filePaths.TryAdd(path,path[0] + path[2..])){
                        int t = path.LastIndexOf('\\',path.Length - 1);
                        directoryPaths.TryAdd(path[0] + path[2..(t+1)],0x00);
                        try{
                            backupSize += (ulong) new FileInfo(path).Length;
                        }catch(Exception e){
                            Utils.PrintAndLog("Error: " + path + " \nReason: " + e.Message);
                        }
                    }else{
                        Logs.WriteLog("Warning: may already exist in list of files to backup: " + path);
                    }
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
            //ask user if size is ok
            if(!GetUserConfirmation(folderPath,backupSize)){
                Console.WriteLine("User didn't continue with backup progress");
                return; //close if "n" or not enough disk space
            }
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
        private static bool GetUserConfirmation(string location, ulong backupSize){
            Console.Write("The file size to be backed up is: {0:N3} Megabytes would you like to continue? (y/n) ", backupSize / 1_000_000f);
            string answer = "";
            try{
                DriveInfo driveInfo = new("" + location[0]);
                if((ulong)driveInfo.AvailableFreeSpace - 1000000 < backupSize){ //require atleast 1 MB of free space left for any unforeseen issues
                    Utils.PrintAndLog("Error: Not enough free space on drive: " + location);
                    return false;
                }
            }catch(Exception e){
                Utils.PrintAndLog("Error: Getting drive info: " + location + " \nReason: " + e);
            }
            while(answer != "y" && answer != "n"){
                Console.Write(">");
                answer = (Console.ReadLine() + "").Trim().ToLower();
                if(answer != "y" && answer != "n"){
                    Console.WriteLine("Please answer with 'y' or 'n'");
                }
            }
            return answer == "y";
        }
        private bool IsFileInPriorBackups(FileInfo file){
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
        private static bool CreateDir(string path){
            try{
                Directory.CreateDirectory(path);
                return true;
            }catch(Exception e){
                Logs.WriteLog("Error: Creating directory: " + path + " - " + e.Message);
                return false;
            }
        }
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
                    if(checkPriorBackups){
                        if(IsFileInPriorBackups(file)){
                            continue;
                        }
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