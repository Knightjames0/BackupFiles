using System.Collections.Concurrent;
using Util;

namespace BackUp{
    public class NewBackup{
        public const ulong MinimumFreeSpaceLeft = 16_000_000;
        internal static bool CheckEnoughDriveSpace(string location, ulong backupSize){
            try{
                DriveInfo driveInfo = new("" + location[0]);
                if((ulong)driveInfo.AvailableFreeSpace - MinimumFreeSpaceLeft < backupSize){ //require atleast 16 MB of free space left for any unforeseen issues
                    Utils.PrintAndLog("Error: Not enough free space on drive: " + location);
                    return false;
                }
                return true;
            }catch(Exception e){
                Utils.PrintAndLog("Error: Getting drive info: " + location + " \nReason: " + e);
                return false;
            }
        }
        /// <summary>
        /// Prints out msg + " [y/n]" and return a y or n from the user
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        internal static bool GetUserConfirmation(string msg){
            Console.Write(msg + " [y/n]");
            string answer = "";
            while(answer != "y" && answer != "n"){
                Console.Write(">");
                answer = (Console.ReadLine() + "").Trim().ToLower();
                if(answer != "y" && answer != "n"){
                    Console.WriteLine("Please answer with 'y' or 'n'");
                }
            }
            return answer == "y";
        }
        internal static bool CreateDirectoryTreeDown(string path){ //should not be called out side of CreateDirectoryTree
            if(!Directory.Exists(path)){
                short t = (short)path.LastIndexOf('\\',path.Length - 2);
                if(t == -1){
                    return CreateDir(path); 
                }
                if(t > 0){
                    if(CreateDirectoryTreeDown(path[..(t+1)])){
                        return CreateDir(path); 
                    }
                }
            }else{
                return true;
            }
            return false;
        }
        /// <summary>
        /// Creates a directory at a specified path
        /// </summary>
        /// <param name="path">path to create directory at</param>
        /// <returns>returns true if it was successful </returns>
        private static bool CreateDir(string path){
            try{
                Directory.CreateDirectory(path);
                return true;
            }catch(Exception e){
                Logs.WriteLog("Error: Creating directory: " + path + " - " + e.Message);
                return false;
            }
        }
    }
}