using Util;

namespace BackUp{
    public class CreateBackup{
        public static bool CreateDirectoryTreeDown(string folderPath, DataPath dp){
            string temp = dp.GetFullPath()[0] + dp.GetFullPath()[2..]; // work around for drive letters turn into folders
            if(!Directory.Exists(folderPath + dp.GetFullPath())){
                int t = temp.LastIndexOf('\\',temp.Length - 2) + 1;
                if(CreateDirectoryTreePart(folderPath, temp[0..t])){
                    return CreateDir(folderPath + temp);
                }
            }

            return true;
        }
        private static bool CreateDirectoryTreePart(string folderPath, string path){ //should not be called out side of CreateDirectoryTree
            int t = path.LastIndexOf('\\',path.Length - 2);
            if(!Directory.Exists(folderPath + path)){
                if(t == -1){
                    return CreateDir(folderPath + path); 
                }
                if(t > 0){
                    if(CreateDirectoryTreePart(folderPath, path[0..(t+1)])){
                        return CreateDir(folderPath + path); 
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
                Logs.WriteLog("Error: error creating directory: " + path);
                return false;
            }
        }
    }


} 