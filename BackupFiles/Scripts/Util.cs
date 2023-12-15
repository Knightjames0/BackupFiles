namespace Util
{
    public struct DataPath
    {
        private readonly char type;
        private readonly char drive;
        private readonly string path;
        private readonly ulong fileSize;
        private static int numberOf = 1;

        public override readonly string ToString()
        {
            return "" + type + ':' + drive + ':' + path;
        }
        public readonly string GetFullPath(){
            return  "" + drive + ':' + path;
        }
        public readonly char GetPathType(){
            return type;
        }
        public readonly ulong GetSize(){
            return fileSize;
        }
        public DataPath(char c, string fullPath)
        {
            //add file size
            type = c;
            drive = fullPath[0];
            path = fullPath[2..];
            numberOf++;
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
        public static string GetTime(){
            return DateTime.Now.ToString() + " : ";
        }
    }
}