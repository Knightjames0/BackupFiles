using Util;

namespace BackUp{
    public class Data{
        private List<DataPath> fileList;
        public Data(){
            fileList = DataFilePaths.ReadData();
            ListFiles();
        }
        public void UpdateList(){
            fileList = DataFilePaths.ReadData();
        }
        private bool HasPath(DataPath data_Path){ // Check if has path already
            bool result = false;
            Parallel.ForEach(fileList, data => {
                if(data == data_Path){
                    result = true;
                }
            });
            return result;
        }
        public void ListFiles(){
            Parallel.ForEach(fileList, data => {
                Console.WriteLine(data.GetFullPath()); // Just print file or dirctory path names for user
            });
            Console.WriteLine("done");
        }
        public void Add(Args args){ //Check if file or directory exists
            if(args.arguments is null){ // Check if their are arguments passed in
                Console.WriteLine("Error no arguments passed in.");
                return;
            }
            foreach(string arg in args.arguments){
                char type;
                if(File.Exists(arg)){
                    type = '-';
                }else if(Directory.Exists(arg)){
                    type = 'd';
                }else{
                    Console.WriteLine("Error occured file or directory path dosen't exist or can't be reached: " + arg);
                    Logs.WriteLog("Error occured file or directory path dosen't exist or can't be reached: " + arg);
                    continue;
                }
                DataPath data = new(type,arg);
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
            if(folderPath[folderPath.Length-1] != '\\'){
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
            CopyFile(folderPath,fileList[0].GetFullPath());
        }
        public static bool CopyFile(string folderPath, string filePath){
            string temp = filePath[0] + filePath[2..]; // work around
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
        public static bool CreateDirectoryTree(string path){
            if(!Directory.Exists(path)){
                
            }
            int t = path.LastIndexOf('\\');

            return false;
        }
    }
}