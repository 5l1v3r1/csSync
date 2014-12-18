using System.Collections.Generic;
using System.IO;

namespace csSync.Core
{
    public class path
    {
        public string InitialDir = "";
        public string[] LikeString = null;

        public Dictionary<string, directoy> Dirs = new Dictionary<string, directoy>();
        public Dictionary<string, file> Files = new Dictionary<string, file>();

        public int NumDirectories = 0;
        public int NumFiles = 0;
        public long TotalBytes;

        /// <summary>
        /// Obtiene la ruta a extraer
        /// </summary>
        /// <param name="dir">Directorio</param>
        /// <param name="likeString">Condición</param>
        /// <param name="allowCleanFolders">Permite o no las carpetas vacias</param>
        /// <returns>Devuelve la ruta</returns>
        public static path GetDirFromPath(string dir, string[] likeString, bool allowCleanFolders)
        {
            path p = new path();
            p.InitialDir = dir;
            p.LikeString = likeString;

            foreach (DirectoryInfo di in new DirectoryInfo(dir).GetDirectories())
            {
                directoy dx = new directoy(p, di, allowCleanFolders);
                if (!allowCleanFolders && dx.Files.Count <= 0) continue;

                p.Dirs.Add(dx.Name, dx);
            }
            foreach (FileInfo di in new DirectoryInfo(dir).GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                file dx = new file(dir, di);
                if (!dx.CheckLike(likeString)) continue;

                p.NumFiles++;
                p.TotalBytes += dx.Info.Length;
                p.Files.Add(dx.Name, dx);
            }
            return p;
        }
    }
}