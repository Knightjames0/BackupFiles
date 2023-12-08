using System.Reflection;
using System.Text;
using Util;

namespace BackUp{
    public class ModifyFiles{
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
                fs.Close();
            }
            catch{
                throw;
            }
        }
        public static bool WriteData(Data_Path data_Paths){ //Write index Data file
            if(!File.Exists(dataFilePath)){
                CreateDataFile();
            }
            try{
                using FileStream fs = new(dataFilePath, FileMode.Append, FileAccess.Write);
                using StreamWriter sw = new(fs);;
                sw.WriteLine(data_Paths.ToString());
                sw.Close();
                fs.Close();
            }
            catch(Exception e){
                Console.WriteLine("Error occured adding: " + data_Paths.path);
                ModifyFiles.WriteLog(e + " at: " + data_Paths.path);
                return false;
            }
            return true;
        }
        public static List<Data_Path> ReadData(){ //Read all indexes from Data file
            List<Data_Path> input = new();
            if(!File.Exists(dataFilePath)){
                CreateDataFile();
                return input;
            }
            try{
                using FileStream fs = new(dataFilePath, FileMode.Open, FileAccess.Read);
                using StreamReader sr = new(fs);
                string? temp;
                while((temp = sr.ReadLine()) is not null){
                    char c = temp[0];
                    Data_Path data = new(c,temp[2..]);
                    input.Add(data);
                }
                sr.Close();
                fs.Close();
            }
            catch(Exception e){
                ModifyFiles.WriteLog(e.ToString());
            }
            return input;
        }
        private static string GetTime(){
            return DateTime.Now.ToString() + " : ";
        }
    }
}