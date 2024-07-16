using Util;

namespace BackUp{
    public class Data{
        public const short MaxFileLength = 320;
        private List<DataPath> fileList;
        public Data(){
            fileList = new();
            UpdateList();
        }
        /// <summary>
        /// add the paths passed in to the fileList if they don't already exist
        /// any that don't will be logged as an error
        /// </summary>
        /// <param name="args"></param>
        public void AddCommand(Args args){
            if(args.arguments is null){ // Check if their are arguments passed in
                Console.WriteLine("Error: no arguments passed in.");
                return;
            }
            if(args.options is not null){
                if(args.options.Count > 1){
                    Console.WriteLine("Error: Invalid options passed in to add.");
                    return;
                }
            }
            foreach(string path in args.arguments){
                if(path.Length > MaxFileLength - 1){
                    Utils.PrintAndLog("Error: Too long of file name: " + path);
                    continue;
                }
                if(path.Length < 3){
                    Utils.PrintAndLog("Error: Too short of file name: " + path);
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
                if(!DataFilePaths.WriteData(data)){
                    continue;
                }
                fileList.Add(data);
                Console.WriteLine("Added: " + data.GetFullPath());
            }
        }
        /// <summary>
        /// check to see if the path specified already exists in the fileList
        /// </summary>
        /// <param name="data_Path">filePath of type DataPath</param>
        /// <returns>returns True if it already exists in fileList</returns>
        private bool HasPath(DataPath data_Path){ // Check if has path already
            bool result = false;
            Parallel.ForEach(fileList, data => {
                if(Equals(data, data_Path)){
                    result = true;
                }
            });
            return result;
        }
        /// <summary>
        /// Lists out all the file paths passed into the fileList
        /// </summary>
        public void ListCommand(){
            if(fileList.Count == 0){
                Console.WriteLine("List is empty");
                return;
            }
            //Sort the data for easy viewing
            DataPath[] fileListSorted = fileList.ToArray();
            Utils.QuickSort(fileListSorted);
            foreach (var path in fileListSorted)
            {
                Console.WriteLine("{0,-80}",path.GetFullPath()); // Just print file or dirctory path names for user
            }
            Console.WriteLine("Done");
        }
        public static void HelpInfo(){
            string[] helpFile = new string[]{
                "List of Commands\n\n",
                "add [file...] - add file paths or directory paths to backup. For file paths with spaces inclose with double quotes\".\n",
                "remove [file...] - remove file paths or directory paths from backup. For file paths with spaces inclose with double quotes \".\n",
                "list - Provides a sorted list of all paths added\n",
                "backup [file] - Creates one of all the files add the inputed location and must have a destination file path.\n",
                "backup -n [file] [file...] -Creates one of all the files add the inputed location and copies only ones that don't exist in other backups.\n",
                "version - Display Version.\n",
            };
            for(int i = 0; i < helpFile.Length; i++){
                Console.Write(helpFile[i]);
            }
        }
        /// <summary>
        /// try to remove a paths from the fileList if they exist in the list
        /// </summary>
        /// <param name="args"></param>
        public void RemoveCommand(Args args){
            if(args.arguments is null || args.arguments.Count == 0){ // Check if their are arguments passed in
                Console.WriteLine("Error: no arguments passed in.");
                return;
            }
            if(args.options is not null){
                if(args.options.Count > 1){
                    Console.WriteLine("Error: Invalid options passed in to remove.");
                    return;
                }
            }
            for (short i = 0; i < args.arguments.Count; i++){
                string path = args.arguments.ElementAt(i);
                if(path.Length > MaxFileLength * 2){
                    Utils.PrintAndLog("Error: Too long of file name: " + path);
                    args.RemoveArgumentAt(i);
                    i--;
                }
                else if(path.Length < 3){
                    Utils.PrintAndLog("Error: Too short of file name: " + path);
                    args.RemoveArgumentAt(i);
                    i--;
                }
            }
            
            DataPath[] dataPaths = new DataPath[args.arguments.Count];
            for (int i = 0; i < dataPaths.Length; i++){
                dataPaths[i] = new DataPath(args.arguments.ElementAt(i));
            }

            bool[]? isRemoved = DataFilePaths.RemoveData(dataPaths);
            if (isRemoved != null){
                for (int i = 0; i < dataPaths.Length; i++){
                    if(isRemoved[i]){
                        Console.WriteLine("Removed: " + dataPaths[i].GetFullPath());
                    }
                }
            }else{
                Console.WriteLine("Error: removing paths from list");
            }
            UpdateList();
        }
        /// <summary>
        /// Reloads the fileList
        /// </summary>
        private void UpdateList(){
            fileList = DataFilePaths.ReadData();
        }
        /// <summary>
        /// provides a method to backup files from the fileList
        /// </summary>
        /// <param name="args"></param>
        public void BackupCommand(Args args){
            if(args.arguments is null){ // Check if their are arguments passed in
                Console.WriteLine("Error: no arguments passed in.");
                return;
            }
            bool checkPriorBackups = false;
            if(args.options is not null){
                if(args.options.Count != 1){
                    Console.WriteLine("Error: Invalid options passed in to backup.");
                    return;
                }
                if(args.options[0] == 'n'){
                    checkPriorBackups = true;
                }else{
                    Console.WriteLine("Error: Invalid option");
                    return;
                }
                
            }
            //add all prior backup paths to list
            List<string> priorBackups = new();
            if(!checkPriorBackups){
                if(args.arguments.Count > 1){
                    Utils.PrintAndLog("Error: too many arguments passed in");
                    return;
                }
            }else{
                if(args.arguments.Count < 2){
                    Utils.PrintAndLog("Error: no prior backups passed in");
                    return;
                }
                for (int i = 1; i < args.arguments.Count; i++)
                {
                    string priorBackupPath = args.arguments.ElementAt(i);
                    if(priorBackupPath[^1] != '\\'){
                        priorBackupPath += '\\';
                    }
                    if(!Directory.Exists(priorBackupPath)){
                        Utils.PrintAndLog("Error: Prior Backup Path doesn't exist: " + priorBackupPath);
                        return;
                    }
                    priorBackups.Add(priorBackupPath);
                }
            }
            //creates folder name
            string folderPath = args.arguments[0];
            if(folderPath[^1] != '\\'){
                folderPath += '\\';
            }
            if(!Directory.Exists(folderPath)){
                Utils.PrintAndLog("Error: Path selected to backup to doesn't exist: " + folderPath);
                return;
            }
            folderPath += "Backup" + Utils.GetDate();
            //Add number if more then one today
            string temp = "";
            ushort count = 1;
            while(Directory.Exists(folderPath + temp)){
                temp = "_" + count; //Check if exists then increment number tile works
                count++;
            }
            folderPath += temp + '\\';
            _ = new NewBackup(fileList, priorBackups, folderPath, checkPriorBackups);
        }
    }
}