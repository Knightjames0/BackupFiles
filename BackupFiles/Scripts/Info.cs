using System.Text;
using static Utils;

namespace BackUp{
    public class Info{
        private const string logPath = @".\Log.log";
        private const string dataFilePath = @".\Data";
        public static void CreateLog(){ //Create Log.log file
            if(!File.Exists(logPath)){
                try{
                    using FileStream fs = File.Create(logPath);
                    char[] chars = (GetTime() + "Beginning of Logging\n").ToCharArray();
                    fs.Write(Encoding.UTF8.GetBytes(chars), 0, chars.Length);
                    fs.Close();
                } catch{
                    throw;
                }
            }
        }
        public static void CreateDataFile(){ //Create Data file
            if(!File.Exists(dataFilePath)){
                try{
                    using FileStream fs = File.Create(dataFilePath);
                    fs.Close();
                } catch{
                    throw;
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
                sw.WriteLine(GetTime() + s);
                sw.Close();
            }
            catch{
                throw;
            }
        }
        public static bool WriteData(string s, char c){ //Write index Data file
            if(!File.Exists(dataFilePath)){
                CreateDataFile();
            }
            try{
                using FileStream fs = new(dataFilePath, FileMode.Append, FileAccess.Write);
                using StreamWriter sw = new(fs);
                string temp = c + ':' + s;
                sw.WriteLine(temp);
                sw.Close();
            }
            catch(Exception e){
                Console.WriteLine("Error occured adding: " + s);
                Info.WriteLog(e + " at: " + s);
                return false;
            }
            return true;
        }
        public static Data_Paths[]? ReadData(){ //Read all indexes from Data file
            if(!File.Exists(dataFilePath)){
                return null;
            }
            List<Data_Paths> input = new();
            try{
                using FileStream fs = new(dataFilePath, FileMode.Append, FileAccess.Read);
                using StreamReader sr = new(fs);
                string? temp;
                while((temp = sr.ReadLine()) is not null){
                    Data_Paths data = new(temp[0],temp[2..]);
                    input.Add(data);
                }
                sr.Close();
            }
            catch{
                throw;
            }
            return input.ToArray();
        }
        private static string GetTime(){
            return DateTime.Now.ToString() + " : ";
        }
    }
}