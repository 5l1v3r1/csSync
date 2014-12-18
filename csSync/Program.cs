using csSync.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace csSync
{
    class Program
    {
        static bool ParalelDir = false;
        static bool ParalelFiles = false;
        static bool Verbose = false;
        static bool CleanEmptyFolders = false;
        static bool CheckFilesEqualDate = true;

        static int Main(string[] a)
        {
            //a = new string[] { "*", "--pF", "--v", "--cEF", "--dSD", @"E:\test1", "E:\\test2" };

            List<string> args = new List<string>();
            if (a != null) args.AddRange(a);

            if (args.Contains("--pF")) { args.Remove("--pF"); ParalelFiles = true; }
            if (args.Contains("--pD")) { args.Remove("--pD"); ParalelDir = true; }
            if (args.Contains("--v")) { args.Remove("--v"); Verbose = true; }
            if (args.Contains("--cEF")) { args.Remove("--cEF"); CleanEmptyFolders = true; }
            if (args.Contains("--dSD")) { args.Remove("--dSD"); CheckFilesEqualDate = false; }

            if (args == null || args.Count != 3)
            {
                Console.WriteLine("csSync [LikeString] [Options] [From folder] [To folder]");
                Console.WriteLine("");
                Console.WriteLine(" Options:");
                Console.WriteLine(" --v     Verbose");
                Console.WriteLine(" --cEF   Cleam empty Folders");
                Console.WriteLine(" --dSD   Dont check same date files [Creation & Modified]");
                Console.WriteLine(" --pD    Parallel directories");
                Console.WriteLine(" --pF    Parallel files");
                return 0;
            }

            string[] LikeString = args[0].Split(new char[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

            string dirOrigen = args[1].TrimEnd('\\', '/');
            if (!Directory.Exists(dirOrigen)) { LIB.WriteLine("[" + dirOrigen + "] must exists"); return 0; }

            string dirDest = args[2].TrimEnd('\\', '/');
            if (!Directory.Exists(dirDest)) { LIB.WriteLine("[" + dirDest + "] must exists"); return 0; }

            LIB.WriteLine("Reading files");
            LIB.WriteLine(" From: '" + dirOrigen + "'");
            LIB.WriteLine(" To: '" + dirDest + "'");

            if (ParalelDir) LIB.WriteLine(" Using: 'Paralell directories'");
            if (ParalelFiles) LIB.WriteLine(" Using: 'Paralell files'");
            if (CleanEmptyFolders) LIB.WriteLine(" Using: 'Clean empty folders'");
            if (!CheckFilesEqualDate) LIB.WriteLine(" Using: 'Dont check same date files'");

            Task<path> taskA = Task.Factory.StartNew<path>(() => path.GetDirFromPath(dirOrigen, LikeString, !CleanEmptyFolders));
            Task<path> taskB = Task.Factory.StartNew<path>(() => path.GetDirFromPath(dirDest, null, true));

            Task.WaitAll(new Task[] { taskA, taskB });

            path org = taskA.Result;
            path dst = taskB.Result;

            Console.WriteLine("");
            LIB.WriteLine("From: ");
            LIB.WriteLine(" Directories: " + org.NumDirectories.ToString());
            LIB.WriteLine(" Files: " + org.NumFiles.ToString());
            LIB.WriteLine(" Size: " + LIB.DoubleAKB(org.TotalBytes, true));
            LIB.WriteLine("To: ");
            LIB.WriteLine(" Directories: " + dst.NumDirectories.ToString());
            LIB.WriteLine(" Files: " + dst.NumFiles.ToString());
            LIB.WriteLine(" Size: " + LIB.DoubleAKB(dst.TotalBytes, true));
            Console.WriteLine("");

            LIB.WriteLine("Start process (wait please)");
            Process(org.Dirs, dst.Dirs, dirDest);
            Process(org.Files, dst.Files, dirDest);
            LIB.WriteLine("End process");

            return 1;
        }

        #region DO
        static void Do(Dictionary<string, directoy> dest, string dirDest, directoy org)
        {
            directoy dst;
            if (dest.TryGetValue(org.Name, out dst))
            {
                // Existe
                dst.AllowDelete = false;
                org.CopyAttributes(dst.Info);

                Process(org.Dirs, dst.Dirs, dirDest);
                Process(org.Files, dst.Files, dirDest);
            }
            else
            {
                // No existe
                if (Verbose) LIB.WriteLine("Copy full folder '" + dirDest + org.PartialPath + "'");
                directoy.CopyTo(org, dirDest);
            }
        }
        static void Do(Dictionary<string, file> dest, string dirDest, file org)
        {
            file dst;
            if (dest.TryGetValue(org.Name, out dst))
            {
                // Existe
                Process(org, dst, dirDest);
            }
            else
            {
                // No existe
                if (Verbose) LIB.WriteLine("Copy full file '" + dirDest + org.PartialPath + "'");
                file.CopyTo(org, dirDest, 0);
            }
        }
        #endregion

        #region PROCESS
        static void Process(file org, file dst, string dirDest)
        {
            dst.AllowDelete = false;

            long desde = org.GetChangePosition(dst, CheckFilesEqualDate);
            if (desde < 0)
            {
                // El archivo es igual
                org.CopyAttributes(dst.Info);
            }
            else
            {
                if (Verbose)
                {
                    if (desde > 0) LIB.WriteLine("Copy partial file [From " + desde.ToString() + "] '" + dirDest + org.PartialPath + "'");
                    else LIB.WriteLine("Copy full file '" + dirDest + org.PartialPath + "'");
                }
                // Copia entero el archivo
                file.CopyTo(org, dirDest, desde);
                org.CopyAttributes(dst.Info);
            }
        }
        static void Process(Dictionary<string, file> origen, Dictionary<string, file> dest, string dirDest)
        {
            if (ParalelFiles)
            {
                Parallel.ForEach<file>(origen.Values, org => { Do(dest, dirDest, org); });
            }
            else
            {
                foreach (file org in origen.Values) Do(dest, dirDest, org);
            }

            // Procesar los eliminar
            if (dest.Count > 0)
            {
                List<file> deletes = new List<file>();
                foreach (file d in dest.Values)
                    if (d.AllowDelete) deletes.Add(d);

                foreach (file d in deletes)
                {
                    d.Delete();
                    dest.Remove(d.Name);
                    if (Verbose) LIB.WriteLine("Delete file '" + dirDest + d.PartialPath + "'");
                }
            }
        }
        static void Process(Dictionary<string, directoy> origen, Dictionary<string, directoy> dest, string dirDest)
        {
            if (origen == null) return;
            if (dest == null) return;

            if (ParalelDir)
            {
                Parallel.ForEach<directoy>(origen.Values, org => { Do(dest, dirDest, org); });
            }
            else
            {
                foreach (directoy org in origen.Values) Do(dest, dirDest, org);
            }

            // Procesar los eliminar
            if (dest.Count > 0)
            {
                List<directoy> deletes = new List<directoy>();
                foreach (directoy d in dest.Values)
                    if (d.AllowDelete) deletes.Add(d);

                foreach (directoy d in deletes)
                {
                    d.Delete();
                    dest.Remove(d.Name);
                    if (Verbose) LIB.WriteLine("Delete folder '" + dirDest + d.PartialPath + "'");
                }
            }
        }
        #endregion
    }
}