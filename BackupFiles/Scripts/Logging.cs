
namespace Util{
    public class Logs{
        private const string logPath = @".\Log.log";
        /// <summary>
        /// Creates the log file at @".\Log.log" located in the same directory as the executable
        /// </summary>
        public static void CreateLog(){
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
        }
        /// <summary>
        /// Write single message to Log.log file
        /// </summary>
        /// <param name="msg">Amended a single message to end of log file</param>
        public static void WriteLog(string msg){
            if(!File.Exists(logPath)){
                CreateLog();
            }
            try{
                using FileStream fs = new(logPath, FileMode.Append, FileAccess.Write);
                using StreamWriter sw = new(fs);
                sw.WriteLine(Utils.GetTime() + msg);
                sw.Close();
                fs.Close();
            }
            catch(Exception e){
                Console.WriteLine("Error: Writing to log file: " + msg + "\n" + e);
            }
        }
        /// <summary>
        /// Write multiple messages to Log.log file
        /// </summary>
        /// <param name="msg">Amended multiply messages in order to end of log file</param>
        public static void WriteLog(string[] msg){
            if(msg.Length == 0){
                return;
            }
            if(!File.Exists(logPath)){
                CreateLog();
            }
            try{
                using FileStream fs = new(logPath, FileMode.Append, FileAccess.Write);
                using StreamWriter sw = new(fs);
                string time = Utils.GetTime();

                for (int i = 0; i < msg.Length; i++){
                    sw.WriteLine(time + msg[i]);
                }
                
                sw.Close();
                fs.Close();
            }
            catch(Exception e){
                Console.WriteLine("Error: Writing to log file: " + msg + "\n" + e);
            }
        }
    }
}