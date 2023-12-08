
namespace BackUp{
    public class Program{
        public struct Args{
            public string command;
            public string[] arguments;
        }
        public static void Main(){
            string input = "";
            Info.CreateLog();
            Info.CreateDataFile();
            Info.WriteLog("New Session Started");
            while(input != "exit"){
                Console.Write(">");
                input = (Console.ReadLine() + "").Trim();
                Args args = ParseArgs(input);
                
                if(args.command == "add"){
                    Command.Add(args.arguments);
                }
            }
        }
        public static Args ParseArgs(string s){
            Args args = new();
            if(s.Length < 1){
                Console.WriteLine("No command found");
                return args;
            }
            int index = s.IndexOf(' ');
            if(index == -1){
                index = s.Length - 1;
                args.command = s[0..index];
                return args;
            }
            args.command = s[0..index];
            List<string> tempList = new();
            bool skipSpace = false;
            string temp = "";
            for(int i = index; i < s.Length; i++){
                char c = s[i];
                if(c == '"'){
                    skipSpace = !skipSpace;
                }else if(!skipSpace && c == ' '){
                    if(temp != ""){
                        tempList.Add(temp);
                        temp = "";
                    }
                    continue;
                }else{
                    temp += c;
                }
            }
            tempList.Add(temp);
            args.arguments = tempList.ToArray();
            return args;
        }
    }
}