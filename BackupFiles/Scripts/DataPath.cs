namespace BackUp_V2{
    public struct DataPath{
        public readonly char fileType;
        private readonly char drive;
        private readonly string path;
        public DataPath(char fileType, string fullPath){
            this.fileType = fileType;
            drive = fullPath[0];
            path = fullPath[2..];
        }
        public DataPath(string fullPath){
            fileType = '?';//invalid
            drive = fullPath[0];
            path = fullPath[2..];
        }
        public override string ToString()
        {
            return "" + fileType + ':' + drive + ':' + path;
        }
        public string GetFullPath(){
            return  "" + drive + ':' + path;
        }
        public bool IsEqual(DataPath other){
            //DataPath isn't nullable
            return this.drive == other.drive && this.path == other.path;
        }
    }
}