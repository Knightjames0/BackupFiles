using static Utils;

namespace BackUp{
    public class Command{
        public static List<Data_Paths> listData = new();

        public static void Add(string[] arguments){ //Check if file or directory exists
            foreach(string arg in arguments){
                if(File.Exists(arg)){
                    Info.WriteData(arg,'-');
                }else if(Directory.Exists(arg)){
                    Info.WriteData(arg,'d');
                }else{
                    Console.WriteLine("Error occured file or directory dosen't exist: " + arg);
                }
            }
        }
    }
}