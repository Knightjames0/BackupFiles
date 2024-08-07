﻿using Util;

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
            
            if(args.command == "add"){
                //data.Add(args);
                data.AddCommand(args);
            }else if(args.command == "remove"){
                //data.Remove(args);
                data.RemoveCommand(args);
            }else if(args.command == "backup"){
                //data.NewBackup(args);
                data.BackupCommand(args);
            }else if(args.command == "list"){
                //data.ListFiles();
                data.ListCommand();
            }else if(args.command == "help"){
                Data.HelpInfo();
                //Data.HelpInfo();
            }else if(args.command == "version"){
                Console.WriteLine("BackupFiles version 1.1.3");
            }else if(args.command == "exit"){
                //it will now close the console
                Console.WriteLine("Closing File Backup System");
                Logs.WriteLog("Session Closed");
                Environment.Exit(0);
            }else if(args.command == ""){
                //handled in Args Constructer
            }else{
                Utils.PrintAndLog("Invalid Command: " + args.command + "\nTry help for a list of commands");
            }
        }
    }
}