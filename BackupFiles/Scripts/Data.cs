using Util;

namespace BackUp{
    public class Data{
        private List<Data_Path> fileList;
        public Data(){
            fileList = ModifyFiles.ReadData();
            foreach(Data_Path data_ in fileList){
                Console.WriteLine(data_.ToString());
            }
        }
        private bool HasPath(Data_Path data_Path){ // Check if has path already
            bool result = false;
            Parallel.ForEach(fileList, data => {
                if(data.path == data_Path.path){
                    result = true;
                }
            });
            return result;
        }
        public void ListFiles(){
            Parallel.ForEach(fileList, data => {
                Console.WriteLine(data.path); // Just print file or dirctory path names for user
            });
        }
        public void Add(string[] arguments){ //Check if file or directory exists
            if(arguments is null){ // Check if their are arguments passed in
                Console.WriteLine("Error no arguments passed in.");
                return;
            }
            foreach(string arg in arguments){
                char type;
                if(File.Exists(arg)){
                    type = '-';
                }else if(Directory.Exists(arg)){
                    type = 'd';
                }else{
                    Console.WriteLine("Error occured file or directory path dosen't exist or can't be reached: " + arg);
                    ModifyFiles.WriteLog("Error occured file or directory path dosen't exist or can't be reached: " + arg);
                    continue;
                }
                Data_Path data = new(type,arg);
                if(!HasPath(data)){ //check if already exists in listData
                    if(ModifyFiles.WriteData(data)){ //only if it is writen to the data file
                        fileList.Add(data);
                    }
                }else{
                    Console.WriteLine("Error already in list of files: " + arg);
                }
            }
        }
    }
}