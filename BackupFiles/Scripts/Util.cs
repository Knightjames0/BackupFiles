using BackUp;

namespace Util
{
    public readonly struct Args{
        public readonly string command;
        public readonly List<char>? options;
        public readonly List<string>? arguments;
        public Args (string inputString){
            if (inputString.Length < 1){
                Utils.PrintAndLog("Error: Nothing passed in");
                command = "";
                return;
            }
            if(inputString.Length > short.MaxValue - 1){
                Utils.PrintAndLog("Error: Command passed in was too long max: " + (short.MaxValue - 1) + " characters.");
                command = "";
                return;
            }
            // Get Command
            short index = (short)inputString.IndexOf(' ');
            if (index == -1){
                command = inputString[0..];
                return;
            }
            command = inputString[0..index];
            if (index + 1 >= inputString.Length){
                return;
            }
            // Get Options
            if(inputString[index+1] == '-'){
                options = new();
                index +=2;
                for (short i = index; i < inputString.Length; i++){
                    if(inputString[i] == ' '){
                        index = i;
                        break;
                    }else{
                        if(!CharExistInList(options, inputString[i])){
                            options.Add(inputString[i]);
                        }else{
                            command = "";
                            Utils.PrintAndLog("Error: Repeating characters for options passed in: " + inputString);
                            return;
                        }
                    }
                    index = (short)(1 + i);
                }
            }
            // Get Arguments
            if (index >= inputString.Length){
                return;
            }
            arguments = new();
            bool skipSpace = false;
            string temp = "";
            for (short i = index; i < inputString.Length; i++){
                char c = inputString[i];
                if (c == '"'){ // " are use when spaces are in a file or directory name
                    skipSpace = !skipSpace;
                }
                else if (!skipSpace && c == ' '){
                    if (temp != "")
                    {
                        if(temp.Length > 0){//prevent empty strings
                            arguments.Add(temp);
                        }
                        temp = "";
                    }
                    continue;
                }else{
                    temp += c;
                }
            }
            if(temp.Length > 0){//prevent empty strings
                arguments.Add(temp);
            }
            return;
        }
        private bool CharExistInList(List<char> options, char c){
            foreach(char value in options){
                if(c == value){
                    return true;
                }
            }
            return false;
        }
        public bool RemoveArgumentAt(short index){
            if(arguments == null){
                return false;
            }
            if(arguments.Count > index){
                arguments.RemoveAt(index);
                return true;
            }
            return false;
        }
    }
    public class Utils{
        public static void PrintAndLog(string msg){
            Console.WriteLine(msg);
            Logs.WriteLog(msg);
        }
        public static string GetTime(){
            return DateTime.Now.ToString() + " : ";
        }
        public static string GetDate(){
            string temp = DateTime.Now.ToShortDateString();
            //int t = temp.IndexOf(' ');
            string[] arr = temp[0..].Split('/');
            temp = "";
            for(int i = 0; i < arr.Length; i++){
                temp += '_' + arr[i];
            }
            return temp;
        }
        /// <summary>
        /// Quick Sort Algorithm for sorting an array of DataPaths
        /// </summary>
        /// <param name="dataPaths"></param>
        public static void QuickSort(DataPath[] dataPaths){
            if(dataPaths == null || dataPaths.Length < 2){
                return;
            }
            InternalQuickSort(dataPaths, 0, dataPaths.Length-1);
        }
        private static void InternalQuickSort(DataPath[] dataPaths, int low, int high){
            if(low >= high){
                return;
            }
            int pivot = InternalQuickSortPivotSelect(dataPaths, low, high);
            DataPath dataPath = dataPaths[pivot];
            DataPath temp;
            for(int i = low; i < pivot;){
                if(dataPath.CompareTo(dataPaths[i]) <= 0){
                    temp = dataPaths[i];
                    pivot--;
                    dataPaths[i] = dataPaths[pivot];
                    dataPaths[pivot] = temp;
                }else{
                    i++;
                }
            }
            dataPaths[high] = dataPaths[pivot];
            dataPaths[pivot] = dataPath;

            InternalQuickSort(dataPaths, low, pivot-1);
            InternalQuickSort(dataPaths, pivot+1, high);
        }
        private static int InternalQuickSortPivotSelect(DataPath[] dataPaths, int low, int high){
            if(high - low < 3){
                return high;
            }
            int mid = (high + low)/2;
            if(dataPaths[low].CompareTo(dataPaths[mid]) <= 0){
                if(dataPaths[mid].CompareTo(dataPaths[high]) < 0){
                    DataPath temp = dataPaths[mid];
                    dataPaths[mid] = dataPaths[high];
                    dataPaths[high] = temp;
                }
            }else{
                if(dataPaths[mid].CompareTo(dataPaths[high]) < 0){
                    DataPath temp = dataPaths[low];
                    dataPaths[low] = dataPaths[high];
                    dataPaths[high] = temp;
                }
            }
            return high;
        }
    }
}