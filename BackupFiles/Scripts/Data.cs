using Util;

namespace BackUp_V2{
    public class Data{
        private List<DataPath> fileList;
        public Data(){
            fileList = new();
            UpdateList();
        }
        public void AddCommand(Args args){
            if(args.arguments is null){ // Check if their are arguments passed in
                Console.WriteLine("Error: no arguments passed in.");
                return;
            }
            foreach(string path in args.arguments){
                if(path.Length > 255){
                    Utils.PrintAndLog("Error: Too long of file name: " + path);
                    continue;
                }
                char fileType;
                string dataPath = path;
                if(File.Exists(path)){
                    fileType = '-';
                }else if(Directory.Exists(path)){
                    fileType = 'd';
                    if(path[^1] != '\\'){
                        dataPath += '\\';
                    }
                }else{
                    Utils.PrintAndLog("Error: path dosen't exist or can't be reached: " + path);
                    continue;
                }
                DataPath data = new(fileType,dataPath);
                if(HasPath(data)){
                    Utils.PrintAndLog("Error: already in list of files: " + path);
                    continue;
                }
                if(NewDataFilePaths.WriteData(data)){
                    fileList.Add(data);
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
        public void ListCommand(){
            if(fileList.Count == 0){
                Console.WriteLine("List is empty");
                return;
            }
            foreach (var item in fileList)
            {
                Console.WriteLine("{0,-80}",item.GetFullPath()); // Just print file or dirctory path names for user
            }
            Console.WriteLine("Done");
        }
        public static void HelpInfo(){
            string[] helpFile = new string[6]{
                "List of Commands\n\n",
                "add [file...] - add file paths or directory paths to backup. For file paths with spaces inclose with (\"\").\n",
                "remove [file...] - remove file paths or directory paths from backup. For file paths with spaces inclose with (\"\").\n",
                "list - provides a list paths added\n",
                "backup [file] - Creates one of all the files add at a inputed location and must have a destination file path.\n",
                "version - Display Version.\n",
            };
            for(int i = 0; i < helpFile.Length; i++){
                Console.Write(helpFile[i]);
            }
        }
        public void RemoveCommand(Args args){
            if(args.arguments is null || args.arguments.Count == 0){ // Check if their are arguments passed in
                Console.WriteLine("Error: no arguments passed in.");
                return;
            }
            for (int i = 0; i < args.arguments.Count; i++){
                string path = args.arguments.ElementAt(i);
                if(path.Length > 256){
                    Console.WriteLine("Error: Too long of file name: " + path);
                    args.RemoveArgumentAt(i);
                    i--;
                }
            }
            DataPath[] dataPaths = new DataPath[args.arguments.Count];
            for (int i = 0; i < dataPaths.Length; i++)
            {
                dataPaths[i] = new DataPath(args.arguments.ElementAt(i));
            }
            if(!NewDataFilePaths.RemoveData(dataPaths)){
                Console.WriteLine("Error: removing paths from list");
            }
            UpdateList();
        }
        private void UpdateList(){
            fileList = NewDataFilePaths.ReadData();
        }
        public void BackupCommand(Args args){
            if(args.arguments is null){ // Check if their are arguments passed in
                Console.WriteLine("Error: no arguments passed in.");
                return;
            }
            string folderPath = args.arguments[0]; //creates folder name
            if(folderPath[^1] != '\\'){
                folderPath += '\\';
            }
            folderPath += "Backup" + Utils.GetDate();;
            //Add number if more then one today
            string temp = "";
            ushort count = 1;
            while(Directory.Exists(folderPath + temp)){
                temp = "_" + count; //Check if exists then increment number tile works
                count++;
            }
            folderPath += temp + '\\';
            //add later
            
            List<string> oldBackups = new();
            NewBackup.Backup(fileList,oldBackups,folderPath);
        }
    }
}