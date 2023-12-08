using Util;

namespace BackUp{
    public class Data{
        private List<Data_Path> listData;
        public Data(){
            listData = ModifyFiles.ReadData();
            foreach(Data_Path data_ in listData){
                Console.WriteLine(data_.ToString());
            }
        }
        public bool HasPath(Data_Path data_Path){ // TODO
            bool result = false;
            Parallel.ForEach(listData, data => {
                if(data.path == data_Path.path){
                    result = true;
                }
            });
            return result;
        }
        
        public void Add(string[] arguments){ //Check if file or directory exists
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
                    ModifyFiles.WriteData(data);
                    listData.Add(data);
                }else{
                    Console.WriteLine("Error already in list of files: " + arg);
                }
            }
        }
    }
}