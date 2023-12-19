using Util;

namespace BackUp{
    public class CreateBackup{
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
            }catch{
                Logs.WriteLog("Error: Creating directory: " + path);
                return false;
            }
        }
        public static void CreateNewDirectoryTree(string destinationPath, string locationPath)
        {
            if(!CreateDirectoryTreeDown(destinationPath)){
                Console.WriteLine("Error: Creating Directories " + destinationPath);
                return;
            }
            DirectoryInfo destination = new(destinationPath);
            DirectoryInfo location = new(locationPath);
            CreateNewDirectoryTreePart(destination,location);
        }
        private static void CreateNewDirectoryTreePart(DirectoryInfo destination, DirectoryInfo location){
            string destinationPath = destination.FullName;
            if(destinationPath.Length > 256){ //don't allow for large file paths to be copied can prevent infinate loops
                Console.WriteLine("Error: Too long of file name: " + location.FullName);
                return;
            }
            try{
                FileInfo[] fileInfos = location.GetFiles(); //copy all files in current directory
                foreach (FileInfo file in fileInfos){
                    CopyFile(destination,file);
                }
                DirectoryInfo[] directories = location.GetDirectories(); // get subdirectories
                foreach (DirectoryInfo dir in directories){
                    string dirPath = destinationPath + dir.Name + "\\";
                    if(!Directory.Exists(dirPath)){
                        CreateDir(dirPath);
                        DirectoryInfo nextDestination = new DirectoryInfo(dirPath);
                        CreateNewDirectoryTreePart(nextDestination, dir);
                    }
                }
            }catch{
                Console.WriteLine("Error: D001: " + location.FullName);
            }
        }
        public static void CreateNewFile(string destinationPath, string locationPath){ //handes file tree and errors
            if(!File.Exists(locationPath)){
                Console.WriteLine("Error: file can't be reached " + locationPath);
            }
            int t = destinationPath.LastIndexOf('\\',destinationPath.Length - 1);
            if(!CreateDirectoryTreeDown(destinationPath[..(t+1)])){
                Console.WriteLine("Error: with " + destinationPath);
                return;
            }
            FileInfo location = new(locationPath);
            DirectoryInfo destination = new(destinationPath[..(t+1)]);
            CopyFile(destination,location);
        }
        public static bool CopyFile(DirectoryInfo destination, FileInfo location){ //Creates file
            if(location is null || destination is null){
                return false;
            }
            if(File.Exists(destination.FullName + location.Name)){
                Logs.WriteLog("Warning: File already exists " + destination.FullName + location.Name);
                return false;
            }
            try{
                File.Copy(location.FullName,destination.FullName + location.Name);
            }catch(Exception e){
                Console.WriteLine("Error: Copying file to backup failed: " + location.FullName + "\n" + e);
            }
            return true;
        }
    }
} 