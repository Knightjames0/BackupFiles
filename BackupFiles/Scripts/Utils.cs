public class Utils{
    public struct Data_Paths{
        public char type;
        public string path;

        public override readonly string ToString(){
            return type + ':' + path;
        }
        public Data_Paths(char c, string path){
            type = c;
            this.path = path;
        }
    }
}