using Util;

namespace BackUp{
    public class DataFilePaths{
        private const string dataFilePath = @".\Data";
        public static void CreateDataFile(){ //Create Data file
            if(!File.Exists(dataFilePath)){
                try{
                    using FileStream fs = File.Create(dataFilePath);
                    fs.Close();
                } catch(Exception e){
                    Console.WriteLine("Error Creating Data file: " + dataFilePath + "\n" + e);
                }
            }
        }
        public static bool WriteData(DataPath data_Paths){ //Write index Data file
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
                Console.WriteLine("Error occured adding: " + data_Paths.GetFullPath());
                Logs.WriteLog(e + " at: " + data_Paths.GetFullPath());
                return false;
            }
            return true;
        }
        public static List<DataPath> ReadData(){ //Read all indexes from Data file
            List<DataPath> input = new();
            if(!File.Exists(dataFilePath)){
                CreateDataFile();
                return input;
            }
            try{
                using FileStream fs = new(dataFilePath, FileMode.Open, FileAccess.Read);
                using StreamReader sr = new(fs);
                string? temp;
                while((temp = sr.ReadLine()) is not null){
                    if(temp.Length < 3){
                        Logs.WriteLog("Error Parsing to short: " + temp);
                        continue;
                    }else if(temp[1] != ':'){
                        Logs.WriteLog("Error Parsing no (':') in: " + temp);
                        continue;
                    }
                    char c = temp[0];
                    DataPath data = new(c,temp[2..]);
                    input.Add(data);
                }
                sr.Close();
                fs.Close();
            }
            catch(Exception e){
                Logs.WriteLog(e.ToString());
            }
            return input;
        }
        public static bool RemoveData(DataPath dataPath){
            //TODO remove old or paths that don't exist anymore
            return false;
        }
    }
}