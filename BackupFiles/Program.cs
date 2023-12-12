using Util;

namespace BackUp{
    public class Program{
        public static void Main(){
            string input = "";
            Logs.CreateLog();
            DataFilePaths.CreateDataFile();
            Logs.WriteLog("New Session Started");
            Data data = new();

            while(input != "exit"){
                Console.Write(">");
                input = (Console.ReadLine() + "").Trim();
                Args args = new(input);
                
                if(args.command == "add"){
                    data.Add(args);
                }else if(args.command == "backup"){
                    data.CreateBackUp(args);
                }else if(args.command == "list"){
                    data.ListFiles();
                }else{
                    Console.WriteLine("Invalid Command: " + args.command);
                }
            }
            Logs.WriteLog("Session Closed");
            Console.WriteLine("Closing File Backup System");
            Thread.Sleep(100);
        }
    }
}