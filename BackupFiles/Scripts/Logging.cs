using System.Text;
using Util;

namespace BackUp{
    public class Logs{
        private const string logPath = @".\Log.log";
        public static void CreateLog(){ //Create Log.log file
            if(!File.Exists(logPath)){
                try{
                    using FileStream fs = File.Create(logPath);
                    char[] chars = (Utils.GetTime() + "Beginning of Logging\n").ToCharArray();
                    fs.Write(Encoding.UTF8.GetBytes(chars), 0, chars.Length);
                    fs.Close();
                } catch(Exception e){
                    Console.WriteLine("Error Creating log file: " + logPath + "\n" + e);
                }
            }
        }
        public static void WriteLog(string s){ // Write to Log.log file
            if(!File.Exists(logPath)){
                CreateLog();
            }
            try{
                using FileStream fs = new(logPath, FileMode.Append, FileAccess.Write);
                using StreamWriter sw = new(fs);
                sw.WriteLine(Utils.GetTime() + s);
                sw.Close();
                fs.Close();
            }
            catch(Exception e){
                Console.WriteLine("Error Writing to log file: " + s + "\n" + e);
            }
        }
    }
}