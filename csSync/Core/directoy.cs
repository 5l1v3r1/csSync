using System.Collections.Generic;
using System.IO;

namespace csSync.Core
{
    public class directoy
    {
        public DirectoryInfo Info;
        public string Name;
        public string FullName;
        public string PartialPath;
        public bool AllowDelete = true;

        public Dictionary<string, directoy> Dirs = null;
        public Dictionary<string, file> Files = null;

        public directoy(string startupPath, DirectoryInfo dx, string[] likeString)
        {
            Info = dx;
            Name = dx.Name;
            FullName = dx.FullName;
            PartialPath = FullName.Remove(0, startupPath.Length);

            Dirs = new Dictionary<string, directoy>();
            foreach (DirectoryInfo d1 in dx.GetDirectories())
            {
                directoy d = new directoy(startupPath, d1, likeString);

                Dirs.Add(d.Name, d);
            }

            Files = new Dictionary<string, file>();
            foreach (FileInfo d1 in dx.GetFiles())
            {
                file f = new file(startupPath, d1);
                f.CheckLike(likeString);

                Files.Add(f.Name, f);
            }
        }
        public override string ToString() { return Name; }

        /// <summary>
        /// Elimina el Directorio
        /// </summary>
        public void Delete()
        {
            try
            {
                clearFolder(FullName);
                Directory.Delete(FullName, true);
            }
            catch { }
        }
        void clearFolder(string FolderName)
        {
            DirectoryInfo dir = new DirectoryInfo(FolderName);

            foreach (FileInfo fi in dir.GetFiles())
                fi.Delete();

            foreach (DirectoryInfo di in dir.GetDirectories())
            {
                clearFolder(di.FullName);
                di.Delete();
            }
        }
        /// <summary>
        /// Copia el directorio completo
        /// </summary>
        /// <param name="dir">Directorio</param>
        /// <param name="dirDest">Destino</param>
        public static void CopyTo(directoy dir, string dirDest)
        {
            try
            {
                DirectoryInfo dest = new DirectoryInfo(dirDest + dir.PartialPath);
                if (!dest.Exists)
                {
                    Directory.CreateDirectory(dest.FullName);
                }
                // Crea el directorio, copiar privilegios
                dir.CopyAttributes(dest);
            }
            catch { }

            foreach (directoy d in dir.Dirs.Values) CopyTo(d, dirDest);
            foreach (file d in dir.Files.Values) file.CopyTo(d, dirDest, 0);
        }

        /// <summary>
        /// Copia los atributos de la carpeta
        /// </summary>
        /// <param name="dest">Destino</param>
        public void CopyAttributes(DirectoryInfo dest)
        {
            // Copiar derechos
            try { Directory.SetAccessControl(dest.FullName, Info.GetAccessControl()); }
            catch { }
            // Copiar atributos
            //try { Directory.SetCreationTime(dest.FullName, Info.CreationTime); }
            //catch { }
            //try { Directory.SetLastAccessTime(dest.FullName, Info.LastAccessTime); }
            //catch { }
            //try { Directory.SetLastWriteTime(dest.FullName, Info.LastWriteTime); }
            //catch { }
            //try { Directory.SetAttributes(dest.FullName, Info.Attributes); }
            //catch { }
        }
    }
}