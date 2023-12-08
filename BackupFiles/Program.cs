using Util;

namespace BackUp{
    public class Program{
        public static void Main(){
            string input = "";
            ModifyFiles.CreateLog();
            ModifyFiles.CreateDataFile();
            ModifyFiles.WriteLog("New Session Started");
            Data data = new();

            while(input != "exit"){
                Console.Write(">");
                input = (Console.ReadLine() + "").Trim();
                Args args = Utils.ParseArgs(input);
                
                if(args.command == "add"){
                    data.Add(args.arguments);
                }
            }
        }
    }
}