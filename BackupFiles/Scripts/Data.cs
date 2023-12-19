using Util;

namespace BackUp{
    public class Data{
        private List<DataPath> fileList;
        public Data(){
            fileList = new();
            UpdateList(false);
        }
        public void UpdateList(bool updateSizes){
            fileList = new(); //check all are valid file paths or directories
            foreach(DataPath dataPath in DataFilePaths.ReadData()){
                char c = dataPath.GetFileType();
                if(c == '-'){
                    if(File.Exists(dataPath.GetFullPath())){
                        if(updateSizes){dataPath.UpdateSize();} //update files size
                        fileList.Add(dataPath);
                        continue;
                    }
                    Logs.WriteLog("Warning: File doesn't exist " + dataPath.ToString());
                }else if(c == 'd'){
                    if(Directory.Exists(dataPath.GetFullPath())){
                        if(updateSizes){dataPath.UpdateSize();} //update files size
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
            UpdateList(true);
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
                    Console.WriteLine("Error: path dosen't exist or can't be reached: " + arg);
                    Logs.WriteLog("Error: path dosen't exist or can't be reached: " + arg);
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
                    Console.WriteLine("Error: already in list of files: " + arg);
                }
            }
        }
        public void NewBackup(Args args){
            if(args.arguments is null){ // Check if their are arguments passed in
                Console.WriteLine("Error: no arguments passed in.");
                return;
            }
            Thread thread = new(() => UpdateList(true));
            thread.Start();
            string folderPath = args.arguments[0]; //creates folder name
            if(folderPath[^1] != '\\'){
                folderPath += '\\';
            }
            folderPath += Utils.GetDate();
            //Add number if more then one today
            folderPath += "Backup\\";
            if(Directory.Exists(folderPath)){
                Console.WriteLine("Error path already exists: " + folderPath);
                thread.Join();
                return;
            }
            thread.Join();
            long backupSize = GetBackupSizeEstimate(); //grabs each size not accounting for if something is contained within
            Console.Write("The file size to be backed up is: {0:N3} Megabytes would you like to continue? (y/n) ", backupSize / 1_000_000f);
            char user = (Console.ReadLine() + "n").ToLower()[0];
            if(user != 'y'){
                Console.WriteLine("User cancelled backup!");
                return;
            }
            int count = fileList.Count;
            int current = count;
            float running = 0f;
            foreach(DataPath dataPath in fileList){
                string temp = dataPath.GetFullPath()[0] + dataPath.GetFullPath()[2..]; // work around for drive letters turn into folders
                if(dataPath.GetFileType() == '-'){ // files
                    CreateBackup.CreateNewFile(folderPath + temp, dataPath.GetFullPath());
                }else if(dataPath.GetFileType() == 'd'){ // directories
                    CreateBackup.CreateNewDirectoryTree(folderPath + temp, dataPath.GetFullPath());
                }else{
                    Console.WriteLine("Error: failed to handle " + dataPath);
                }
                current--;
                float progress = 1f - (float)current / count;
                if(progress > running){
                    Console.WriteLine("Progress: {0:P1}",progress);
                    running = progress + 0.099f;
                }
            }
            Console.WriteLine("Created at: " + folderPath);
        }
        public static void HelpInfo(){
            string[] helpFile = new string[4]{
                "Help info\n",
                "add - add file paths or directory paths. \nadd [file...] - for file paths with spaces inclose with (\"\").\n",
                "list - provides a list paths added\n",
                "backup - creates one of all the files add at a inputed location. \nbackup [file] - must have a destination file path.\n",
            };
            for(int i = 0; i < helpFile.Length; i++){
                Console.Write(helpFile[i]);
            }
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
    }
}