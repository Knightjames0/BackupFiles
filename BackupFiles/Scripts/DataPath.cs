namespace BackUp{
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
        /// <summary>
        /// Checks if the instance is placed before, after or same to the other DataPath
        /// </summary>
        /// <param name="other"></param>
        /// <returns> Value in form of Int32
        /// <para>Less than zero – This instance is before other.</para>
        /// <para>Zero – This instance is the same as other.</para>
        /// <para>Greater than zero – This instance is after other.</para></returns>
        public int CompareTo(DataPath other){
            //DataPath isn't nullable
            if(this.drive > other.drive){
                return 1;
            }
            if(this.drive < other.drive){
                return -1;
            }
            return this.path.CompareTo(other.path);
        }
    }
}