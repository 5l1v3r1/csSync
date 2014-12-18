using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using System.IO;
using System.Security.AccessControl;

namespace csSync.Core
{
    public class file
    {
        const int BUFFER_LENGTH = 1024 * 4;

        public FileInfo Info;
        public string Name;
        public string FullName;
        public string PartialPath;

        public bool AllowDelete = true;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="startupPath">Ruta de inicio</param>
        /// <param name="fx">Información de archivo</param>
        public file(string startupPath, FileInfo fx)
        {
            Name = fx.Name;
            FullName = fx.FullName;
            PartialPath = FullName.Remove(0, startupPath.Length);
            Info = fx;
        }

        /// <summary>
        /// Devuelve true si Contiene el Patron
        /// </summary>
        /// <param name="pattern">Patron</param>
        /// <returns>Devuelve true si contiene el patron</returns>
        public bool CheckLike(string[] pattern)
        {
            int l = pattern == null ? 0 : pattern.Length;

            for (int x = 0; x < l; x++)
            {
                if (!Operators.LikeString(Name, pattern[x], CompareMethod.Text))
                    return false;
            }

            return true;
        }

        public override string ToString() { return Name; }

        /// <summary>
        /// Elimina el archivo
        /// </summary>
        public void Delete()
        {
            try { File.Delete(FullName); }
            catch { }
        }
        /// <summary>
        /// Copia el archivo al destinno
        /// </summary>
        /// <param name="file">File</param>
        /// <param name="dirDest">Ruta destino</param>
        public static void CopyTo(file file, string dirDest, long desde)
        {
            try
            {
                FileInfo dest = new FileInfo(dirDest + file.PartialPath);

                if (desde == 0)
                {
                    // Copia total del archivo
                    File.Copy(file.FullName, dest.FullName, true);
                }
                else
                {
                    // Copia parcial del archivo
                    using (FileStream fo = File.OpenRead(file.FullName))
                    using (FileStream fd = File.OpenWrite(dest.FullName))
                    {
                        fd.SetLength(desde);
                        fo.Seek(desde, SeekOrigin.Begin);
                        fd.Seek(desde, SeekOrigin.Begin);

                        byte[] data = new byte[BUFFER_LENGTH];

                        int lee = 0;
                        while ((lee = fo.Read(data, 0, BUFFER_LENGTH)) != 0)
                            fd.Write(data, 0, lee);
                    }
                }
                // Crea el archivo, copiar privilegios
                file.CopyAttributes(dest);
            }
            catch { }
        }
        /// <summary>
        /// Copia los atributos de la carpeta
        /// </summary>
        /// <param name="dest">Destino</param>
        public void CopyAttributes(FileInfo dest)
        {
            // Copiar derechos
            try
            {
                FileSecurity sec = Info.GetAccessControl();
                sec.SetAccessRuleProtection(true, true);
                dest.SetAccessControl(sec);
            }
            catch { }

            // Copiar atributos
            try
            {
                if (dest.CreationTime != Info.CreationTime)
                    dest.CreationTime = Info.CreationTime;
            }
            catch { }
            //try
            //{
            //    if (dest.LastAccessTime != Info.LastAccessTime)
            //        dest.LastAccessTime = Info.LastAccessTime;
            //}
            //catch { }
            try
            {
                if (dest.LastWriteTime != Info.LastWriteTime)
                    dest.LastWriteTime = Info.LastWriteTime;
            }
            catch { }
            try
            {
                if (dest.Attributes != Info.Attributes)
                    dest.Attributes = Info.Attributes;
            }
            catch { }
        }
        /// <summary>
        /// Devuelve si el arhivo es igual o distinto
        /// </summary>
        /// <param name="dst">Archivo destino</param>
        /// <param name="checkFilesEqualDate">True para comprobar archivos de igual fecha</param>
        /// <returns>Devuelve -1 si el archivo es igual, de lo contrario la posición desde donde cambia</returns>
        public long GetChangePosition(file dst, bool checkFilesEqualDate)
        {
            if (!dst.Info.Exists)
                return 0;

            if (Info.Length == dst.Info.Length)
            {
                if (!checkFilesEqualDate)
                {
                    // Tienen la misma fecha por lo que se omiten
                    if (Info.CreationTime == dst.Info.CreationTime &&
                        Info.LastAccessTime == dst.Info.LastAccessTime)
                        return -1;
                }

                // Leer byte a byte
                byte[] bo = new byte[BUFFER_LENGTH];
                byte[] bd = new byte[BUFFER_LENGTH];

                long pos = 0;
                using (FileStream fo = File.OpenRead(FullName))
                using (FileStream fd = File.OpenRead(dst.FullName))
                {
                    int leo, led;

                    do
                    {
                        leo = fo.Read(bo, 0, BUFFER_LENGTH);
                        led = fd.Read(bd, 0, BUFFER_LENGTH);

                        if (leo != led) return pos;

                        for (int x = 0; x < BUFFER_LENGTH; x++)
                            if (bo[x] != bd[x])
                                return pos + x;

                        pos += BUFFER_LENGTH;
                    }
                    while (leo > 0 && led > 0);
                }

                return -1;
            }

            return 0;
        }
    }
}