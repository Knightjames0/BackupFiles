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
                ParseUserInput(input, data);
            }
            Logs.WriteLog("Session Closed");
            Console.WriteLine("Closing File Backup System");
        }
        public static void ParseUserInput(string input, Data data){
            Args args = new(input);
            switch(args.command){
                case "add":
                    data.AddCommand(args);
                break;
                case "remove":
                    data.RemoveCommand(args);
                break;
                case "backup":
                    data.BackupCommand(args);
                break;
                case "list":
                    data.ListCommand();
                break;
                case "help":
                    Data.HelpInfo();
                break;
                case "version":
                    Data.Version();
                break;
                case "exit":
                    Console.WriteLine("Closing File Backup System");
                    Logs.WriteLog("Session Closed");
                    Environment.Exit(0);
                break;
                default:
                    Utils.PrintAndLog("Invalid Command: " + args.command + "\nTry help for a list of commands");
                break;
            }
        }
    }
}