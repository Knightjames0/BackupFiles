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
                }else if(args.command == "backup"){
                    // TODO create new files from existing
                }else if(args.command == "list"){
                    data.ListFiles();
                }else{
                    Console.WriteLine("Invalid Command: " + args.command);
                }
            }
            ModifyFiles.WriteLog("Session Closed");
            Console.WriteLine("Closing File Backup System");
            Thread.Sleep(100);
        }
    }
}