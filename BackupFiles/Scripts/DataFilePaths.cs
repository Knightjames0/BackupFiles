using Util;

namespace BackUp_V2{
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
                sw.WriteLine(data_Paths);
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
                    if(temp.Length < 4){
                        Logs.WriteLog("Error Parsing to short: " + temp);
                        continue;
                    }else if(temp[1] != ':'){
                        Logs.WriteLog("Error Parsing no (':') in: " + temp);
                        continue;
                    }
                    char c = temp[0];
                    DataPath data = new(c,temp[2..]);
                    bool hasAllReady = false;
                    foreach(DataPath dataPath in input){
                        if(data.IsEqual(dataPath)){
                            Logs.WriteLog("Duplicate in data: " + temp);
                            hasAllReady = true;
                            break;
                        }
                    }
                    if(!hasAllReady){
                        input.Add(data);
                    }
                }
                sr.Close();
                fs.Close();
            }
            catch(Exception e){
                Logs.WriteLog(e.ToString());
            }
            return input;
        }
        public static bool RemoveData(DataPath[] dataPaths){
            string tempFile = Path.GetTempFileName();
            bool[] isFound = new bool[dataPaths.Length];
            string[] paths = new string[dataPaths.Length];
            for (int i = 0; i < dataPaths.Length; i++){
                paths[i] = dataPaths[i].GetFullPath();
            }
            try{
                List<string> workingLines = new();
                using StreamReader sr = new(dataFilePath);
                string? temp;
                while((temp = sr.ReadLine()) is not null){
                    if(temp.Length < 4){ // check for errors in data file and remove them
                        continue;
                    }else if(temp[1] != ':'){
                        continue;
                    }
                    if(!ExistsInData(isFound,paths,temp)){
                        workingLines.Add(temp); // add back to temp file if passes all checks
                    }
                }
                sr.Close();
                File.WriteAllLines(tempFile,workingLines);

                for(int i = 0; i < dataPaths.Length; i++){ // Write Out path that where not found
                    if(!isFound[i]){
                        Console.WriteLine("Error: path is not in list already: " + paths[i]);
                    }
                }

                File.Move(tempFile,dataFilePath,true);
                File.Delete(tempFile);
            }catch(Exception e){
                Logs.WriteLog(e.ToString());
                return false;
            }
            return true;
        }
        private static bool ExistsInData(bool[] isFound, string[] paths, string temp){
            for(int i = 0; i < paths.Length; i++){ //checks if path equals line in data file
                if(temp[2..] == paths[i] || temp[2..] == paths[i] + "\\"){
                    isFound[i] = true;
                    return true;  
                }
            }
            return false;
        }
    }
}