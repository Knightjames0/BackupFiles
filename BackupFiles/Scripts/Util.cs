namespace Util
{
    public struct DataPath
    {
        private readonly char fileType;
        private readonly char drive;
        private readonly string path;
        private long fileSize = 0;

        public override string ToString()
        {
            return "" + fileType + ':' + drive + ':' + path;
        }
        public readonly string GetFullPath(){
            return  "" + drive + ':' + path;
        }
        public readonly char GetFileType(){
            return fileType;
        }
        public readonly long GetFileSize(){
            return fileSize;
        }
        public void UpdateSize(){
            try{
                if(fileType == 'd'){
                    DirectoryInfo directoryInfo = new(GetFullPath());
                    fileSize = Utils.GetDirectorySize(directoryInfo);
                }else if(fileType == '-'){
                    FileInfo fileInfo = new(GetFullPath());
                    fileSize = fileInfo.Length;
                }
            }catch{
                BackUp.Logs.WriteLog("Error: can't reach file path " + ToString());
            }
        }
        public DataPath(char c, string fullPath)
        {
            fileType = c;
            drive = fullPath[0];
            path = fullPath[2..];
            UpdateSize();
        }
        public static bool Equal(DataPath a, DataPath b){
            return a.GetFullPath() == b.GetFullPath();
        }
    }
    public readonly struct Args
    {
        public readonly string command;
        public readonly string? options;
        public readonly string[]? arguments;
        public Args (string s)
        {
            if (s.Length < 1)
            {
                command = "";
                return;
            }
            // Get Command
            int index = s.IndexOf(' ');
            if (index == -1)
            {
                command = s[0..];
                return;
            }
            command = s[0..index];
            // Get Arguments
            List<string> tempList = new();
            bool skipSpace = false;
            string temp = "";
            for (int i = index; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '"') // " are use when spaces are in a file or directory name
                {
                    skipSpace = !skipSpace;
                }
                else if (!skipSpace && c == ' ')
                {
                    if (temp != "")
                    {
                        tempList.Add(temp);
                        temp = "";
                    }
                    continue;
                }
                else
                {
                    temp += c;
                }
            }
            tempList.Add(temp);
            arguments = tempList.ToArray();
            return;
        }
    }
    public class Utils
    {
        public static long GetDirectorySize(DirectoryInfo directory){
            long size = 0;
            try{
                FileInfo[] fileInfos = directory.GetFiles();
                foreach (FileInfo file in fileInfos){
                    size += file.Length;
                }
                DirectoryInfo[] directories = directory.GetDirectories();
                Parallel.ForEach<DirectoryInfo,long>(directories, 
                    () => 0,
                    (i, loop, incr) =>{
                        incr += GetDirectorySize(i);
                        return incr;
                },
                incr => Interlocked.Add(ref size, incr));
            }catch (UnauthorizedAccessException e){
                Console.WriteLine("Error: Calculating folder size with: \n" + e.Message);
            }
            return size;
            
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
                temp += arr[i] + '_';
            }
            return temp;
        }
    }
}