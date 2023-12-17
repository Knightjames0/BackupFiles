using Util;

namespace BackUp{
    public class Data{
        private List<DataPath> fileList;
        public Data(){
            fileList = new();
            UpdateList();
            ListFiles();
        }
        public void UpdateList(){
            fileList = new(); //check all are valid file paths or directories
            foreach(DataPath dataPath in DataFilePaths.ReadData()){
                char c = dataPath.GetFileType();
                if(c == '-'){
                    if(File.Exists(dataPath.GetFullPath())){
                        dataPath.UpdateSize(); //update files size
                        fileList.Add(dataPath);
                        continue;
                    }
                    Logs.WriteLog("Warning: File doesn't exist " + dataPath.ToString());
                }else if(c == 'd'){
                    if(Directory.Exists(dataPath.GetFullPath())){
                        dataPath.UpdateSize(); //update files size
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
                Console.WriteLine("{0,-80} {1:N0} bytes",data.GetFullPath(),data.GetFileSize()); // Just print file or dirctory path names for user
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
        public void NewBackup(Args args){
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
            thread.Join();
            long backupSize = GetBackupSizeEstimate();
            Console.WriteLine("The file size to be backed up is: {0:N2} Kilobytes would you like to continue? (y/n)", backupSize / 1000f);
            char user = (Console.ReadLine() + "n").ToLower()[0];
            if(user != 'y'){
                Console.WriteLine("User cancelled backup!");
                return;
            }
            Directory.CreateDirectory(folderPath);
            Console.WriteLine("Created at: " + folderPath);
            
            foreach(DataPath dataPath in fileList){

                if(!CreateBackup.CreateDirectoryTreeDown(folderPath,dataPath)){
                    Console.WriteLine("Error: with " + dataPath);
                    continue;
                }
                if(dataPath.GetFileType() == 'd'){
                    CreateDirectoryTreeUp(folderPath);
                }
            }
            //CopyFile(folderPath,fileList[0].GetFullPath());
        }
        private long GetBackupSizeEstimate(){
            long size = 0;
            Parallel.ForEach<DataPath,long>(fileList, 
                    () => 0,
                    (file, loop, incr) =>{
                        incr += file.GetFileSize();
                        return incr;
                },
                incr => Interlocked.Add(ref size, incr));
            return size;
        }
        private void CreateDirectoryTreeUp(string folderPath)
        {
            
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
    }
}