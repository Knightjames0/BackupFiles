namespace BackUp{
    public struct DataPath{
        public readonly char FileType;
        public readonly char Drive;
        public readonly string Path;
        public DataPath(char fileType, string fullPath){
            this.FileType = fileType;
            Drive = fullPath[0];
            Path = fullPath[3..];
        }
        public DataPath(string fullPath){
            FileType = '?';//invalid
            Drive = fullPath[0];
            Path = fullPath[3..];
        }
        public override string ToString()
        {
            return "" + FileType + ':' + Drive + ":\\" + Path;
        }
        public string GetFullPath(){
            return  "" + Drive + ":\\" + Path;
        }
        public bool IsEqual(DataPath other){
            //DataPath isn't nullable
            return this.Drive == other.Drive && this.Path == other.Path;
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
            if(this.Drive > other.Drive){
                return 1;
            }
            if(this.Drive < other.Drive){
                return -1;
            }
            return this.Path.CompareTo(other.Path);
        }
    }
}