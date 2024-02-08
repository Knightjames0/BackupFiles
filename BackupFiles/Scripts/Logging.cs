
namespace Util{
    public class Logs{
        private const string logPath = @".\Log.log";
        public static void CreateLog(){ //Create Log.log file
            if(!File.Exists(logPath)){
                try{
                    using FileStream fs = File.Create(logPath);
                    using StreamWriter sw = new(fs);
                    sw.WriteLine(Utils.GetTime() + "Beginning of logging");
                    sw.Close();
                    fs.Close();
                } catch(Exception e){
                    Console.WriteLine("Error Creating log file: " + logPath + "\n" + e);
                }
            }
        }        public static void WriteLog(string s){ // Write to Log.log file
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