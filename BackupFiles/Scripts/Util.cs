namespace Util
{
    public struct Data_Path
    {
        public char type;
        public string path;

        public override readonly string ToString()
        {
            return "" + type + ':' + path;
        }
        public Data_Path(char c, string path)
        {
            type = c;
            this.path = path;
        }
    }
    public struct Args
    {
        public string command;
        public string[] arguments;
    }
    public class Utils
    {
        public static Args ParseArgs(string s)
        {
            Args args = new();
            if (s.Length < 1)
            {
                Console.WriteLine("No command found");
                return args;
            }
            // Get Command
            int index = s.IndexOf(' ');
            if (index == -1)
            {
                args.command = s[0..];
                return args;
            }
            args.command = s[0..index];
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
            args.arguments = tempList.ToArray();
            return args;
        }
    }
}