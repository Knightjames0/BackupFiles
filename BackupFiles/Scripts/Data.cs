using Util;

namespace BackUp{
    public class Data{
        private List<DataPath> fileList;
        public Data(){
            fileList = DataFilePaths.ReadData();
            ListFiles();
        }
        public void UpdateList(){

            fileList = new(); //check all are valid file paths or directories
            foreach(DataPath dataPath in DataFilePaths.ReadData()){
                char c = dataPath.GetPathType();
                if(c == '-'){
                    if(File.Exists(dataPath.GetFullPath())){
                        fileList.Add(dataPath);
                        continue;
                    }
                    Logs.WriteLog("Warning: File doesn't exist " + dataPath.ToString());
                }else if(c == 'd'){
                    if(Directory.Exists(dataPath.GetFullPath())){
                        fileList.Add(dataPath);
                        continue;
                    }
                    Logs.WriteLog("Warning: Directory doesn't exist " + dataPath.ToString());
                }
            }
        }
        private bool HasPath(DataPath data_Path){ // Check if has path already
            bool result = false;
            Parallel.ForEach(fileList, data => {
                if(Equals(data, data_Path)){
                    result = true;
                }
            });
            return result;
        }
        public void ListFiles(){
            Parallel.ForEach(fileList, data => {
                Console.WriteLine(data.GetFullPath()); // Just print file or dirctory path names for user
            });
            Console.WriteLine("Done");
        }
        public void Add(Args args){ //Check if file or directory exists
            if(args.arguments is null){ // Check if their are arguments passed in
                Console.WriteLine("Error: no arguments passed in.");
                return;
            }
            foreach(string arg in args.arguments){
                char type;
                string temp = arg;
                if(File.Exists(arg)){
                    type = '-';
                }else if(Directory.Exists(arg)){
                    type = 'd';
                }else{
                    Console.WriteLine("Error: occured file or directory path dosen't exist or can't be reached: " + arg);
                    Logs.WriteLog("Error: occured file or directory path dosen't exist or can't be reached: " + arg);
                    continue;
                }
                if(arg[^1] != '\\' && type == 'd'){
                    temp += '\\';
                }
                DataPath data = new(type,temp);
                if(!HasPath(data)){ //check if already exists in listData
                    if(DataFilePaths.WriteData(data)){ //only if it is writen to the data file
                        fileList.Add(data);
                    }
                }else{
                    Console.WriteLine("Error already in list of files: " + arg);
                }
            }
        }
        public void CreateBackUp(Args args){
            if(args.arguments is null){ // Check if their are arguments passed in
                Console.WriteLine("Error no arguments passed in.");
                return;
            }
            Thread thread = new(UpdateList);
            thread.Start();
            string folderPath = args.arguments[0];
            if(folderPath[^1] != '\\'){
                folderPath += '\\';
            }
            //Add Today's Date
            //Add number if more then one today
            folderPath += "_Backup\\";
            if(Directory.Exists(folderPath)){
                Console.WriteLine("Error path already exists: " + folderPath);
                thread.Join();
                return;
            }
            Directory.CreateDirectory(folderPath);
            thread.Join();
            Console.WriteLine("Created at: " + folderPath);
            foreach(DataPath dataPath in fileList){
                CreateDirectoryTree(folderPath,dataPath);
            }
            //CopyFile(folderPath,fileList[0].GetFullPath());
        }
        public static bool CopyFile(string folderPath, string filePath){
            string temp = filePath[0] + filePath[2..]; // work around for drive letters
            if(!File.Exists(filePath)){
                Console.WriteLine("fail1");
                return false;
            }
            if(File.Exists(folderPath + temp)){
                Console.WriteLine("fail2");
                return false;
            }
            try{
                File.Copy(filePath, folderPath + temp);
                Console.WriteLine("Created");
            }catch{
                Console.WriteLine("fail3");
                throw;
            }
            return true;
        }
        public static bool CreateDirectoryTree(string folderPath, DataPath dp){
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