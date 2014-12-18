using System.Collections.Generic;

namespace csSync.Core
{
    public class path
    {
        public Dictionary<string, directoy> Dirs = new Dictionary<string, directoy>();
        public Dictionary<string, file> Files = new Dictionary<string, file>();
    }
}