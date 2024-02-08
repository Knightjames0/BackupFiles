using System.Collections.Concurrent;
using System.Collections.Generic;
using Util;

namespace BackUp_V2{
    public class Backup{
        public static void BackupCreate(List<DataPath> dataPaths, List<string> oldBackups, string location){
            if(dataPaths.Count == 0){
                Console.WriteLine("No files to backup added");
            }
            ulong backupSize = 0;
            //LoadBackup
            ConcurrentDictionary<string,string> filePaths = new(16,128);
            // key = path on device, Value = path in backup
            Dictionary<string, byte> directoryPaths = new(32);
            // Value = path in backup
            for (int i = 0; i < dataPaths.Count; i++)
            {
                DataPath dataPath = dataPaths.ElementAt(i);
                string path = dataPath.GetFullPath();
                if(dataPath.fileType == '-'){//file
                    if(!File.Exists(path)){
                        Console.WriteLine("Error: file doesn't exist: " + path);
                        Logs.WriteLog("Error: file doesn't exist: " + path);
                        continue;
                    }
                    if(filePaths.TryAdd(path,path[0] + "\\" + path[2..])){
                        int t = path.LastIndexOf('\\',path.Length - 1);
                        directoryPaths.TryAdd(path[0] + "\\" + path[2..(t+1)],0x00);
                        try{
                            backupSize += (ulong) new FileInfo(path).Length;
                        }catch(Exception e){
                            Console.WriteLine("Error: " + path + " \nReason: " + e.Message);
                            Logs.WriteLog("Error: " + path + " \nReason: " + e.Message);
                        }
                    }else{
                        Logs.WriteLog("Warning: may already exist in list of files to backup: " + path);
                    } 
                }
                else if(dataPath.fileType == 'd'){//directory tree
                    if(!Directory.Exists(path)){
                        Console.WriteLine("Error: directory doesn't exist: " + path);
                        Logs.WriteLog("Error: directory doesn't exist: " + path);
                        continue;
                    }
                    //TODO add files in directories to list
                    backupSize += CreateDirectoryTreeUp(filePaths,directoryPaths,path);
                }
                else{
                    Console.WriteLine("Error: failed to handle: " + path);
                    Logs.WriteLog("Error: failed to handle: " + path);
                }
            }
            //ask user if size is ok
            Console.Write("The file size to be backed up is: {0:N3} Megabytes would you like to continue? (y/n) ", backupSize / 1_000_000f);
            if(!GetUserConfirmation()){
                return; //close if "n"
            }
            //Create directory tree
            CreateDirectoryTreeDown(location);
            foreach(var dirPath in directoryPaths){
                CreateDirectoryTreeDown(location + dirPath.Key);
            }
            Console.WriteLine("File Tree Built");
            //Copy Files
            Parallel.ForEach(filePaths,file => {
                string device = file.Key;
                string backup = location + file.Value;
                CopyFile(device,backup);
            });
            Console.WriteLine("Backup Complete at: " + location);
        }
        private static void CopyFile(string device, string backup){
            if(File.Exists(backup)){
                Logs.WriteLog("Warning: File already exists " + backup);
            }else{
                try{
                    File.Copy(device,backup);
                }catch(Exception e){
                    Console.WriteLine("Error: Copying file to backup failed: " + device + " \nReason: " + e);
                    Logs.WriteLog("Error: Copying file to backup failed: " + device + " \nReason: " + e);
                }
            }
        }
        private static bool GetUserConfirmation(){
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
        private static bool CreateDirectoryTreeDown(string path){ //should not be called out side of CreateDirectoryTree
            if(!Directory.Exists(path)){
                int t = path.LastIndexOf('\\',path.Length - 2);
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
        private static ulong CreateDirectoryTreeUp(ConcurrentDictionary<string, string> filePaths, Dictionary<string, byte> directoryPaths, string sourcePath){
            if(sourcePath.Length > 256){ //don't allow for large file paths to be copied can prevent infinate loops
                Console.WriteLine("Error: Too long of file name: " + sourcePath);
                Logs.WriteLog("Error: Too long of file name: " + sourcePath);
                return 0;
            }
            if(!directoryPaths.TryAdd(sourcePath[0] + "\\" + sourcePath[2..],0x00)){
                Logs.WriteLog("Warning: Directory already added: " + sourcePath);
                return 0;
            }
            ulong size = 0;
            try{
                DirectoryInfo directoryInfo = new(sourcePath);
                FileInfo[] fileInfos = directoryInfo.GetFiles(); //get all files in current directory
                foreach (FileInfo file in fileInfos){
                    if(filePaths.TryAdd(file.FullName, sourcePath[0] + "\\" + sourcePath[2..] + file.Name)){
                        size += (ulong)file.Length;
                    }
                }
                //fix
                DirectoryInfo[] directories = directoryInfo.GetDirectories(); // get subdirectories
                foreach (DirectoryInfo dir in directories){
                    string dirPath = dir.FullName;
                    if(Directory.Exists(dirPath)){
                        size += CreateDirectoryTreeUp(filePaths,directoryPaths,dirPath);
                    }
                }
            }catch(Exception e){
                Console.WriteLine("Error: " + sourcePath + " \nReason: " + e.Message);
                Logs.WriteLog("Error: " + sourcePath + " \nReason: " + e.Message);
            }
            return size;
        }
    }
}