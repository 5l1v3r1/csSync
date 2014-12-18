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

        static int Main(string[] a)
        {
            //a = new string[] { "*", "--pF", @"E:\cygwin_skipfish-2.10b_for_windows", "E:\\test2\\" };

            List<string> args = new List<string>();
            if (a != null) args.AddRange(a);

            if (args.Contains("--pF")) { args.Remove("--pF"); ParalelFiles = true; }
            if (args.Contains("--pD")) { args.Remove("--pD"); ParalelDir = true; }

            if (args == null || args.Count != 3)
            {
                Console.WriteLine("csSync [LikeString] [--pD Parallel Directories] [--pF Parallel Files] [DirectoryOrigen] [DirectoryDest]");
                return 0;
            }

            string[] likeString = args[0].Split(new char[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

            string dirOrigen = args[1].TrimEnd('\\', '/');
            if (!Directory.Exists(dirOrigen))
            {
                Console.WriteLine("[Directory] must exists");
                return 0;
            }
            string dirDest = args[2].TrimEnd('\\', '/');
            if (!Directory.Exists(dirDest))
            {
                Console.WriteLine("[Directory] must exists");
                return 0;
            }

            Task<path> taskA = Task.Factory.StartNew<path>(() => GetDirFromPath(dirOrigen, likeString));
            Task<path> taskB = Task.Factory.StartNew<path>(() => GetDirFromPath(dirDest, likeString));

            Task.WaitAll(new Task[] { taskA, taskB });

            path org = taskA.Result;
            path dst = taskB.Result;

            Process(org.Dirs, dst.Dirs, dirDest);
            Process(org.Files, dst.Files, dirDest);

            return 1;
        }
       
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
                directoy.CopyTo(org, dirDest);
            }
        }
        static void Do(Dictionary<string, file> dest, string dirDest, file org)
        {
            if (!org.LikeOk) return;

            file dst;
            if (dest.TryGetValue(org.Name, out dst))
            {
                // Existe
                Process(org, dst, dirDest);
            }
            else
            {
                // No existe
                file.CopyTo(org, dirDest, 0);
            }
        }

        static void Process(file org, file dst, string dirDest)
        {
            dst.AllowDelete = false;

            long desde = org.GetChangePosition(dst);
            if (desde < 0)
            {
                // El archivo es igual
                org.CopyAttributes(dst.Info);
            }
            else
            {
                // Copia entero el archivo
                file.CopyTo(org, dirDest, desde);
                org.CopyAttributes(dst.Info);
            }
        }
        static void Process(Dictionary<string, file> origen, Dictionary<string, file> dest, string dirDest)
        {
            if (ParalelFiles)
            {
                ParallelLoopResult res = Parallel.ForEach<file>(origen.Values, org => { Do(dest, dirDest, org); });
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
                }
            }
        }
        static void Process(Dictionary<string, directoy> origen, Dictionary<string, directoy> dest, string dirDest)
        {
            if (origen == null) return;
            if (dest == null) return;

            if (ParalelDir)
            {
                ParallelLoopResult res = Parallel.ForEach<directoy>(origen.Values, org => { Do(dest, dirDest, org); });
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
                }
            }
        }
       
        static path GetDirFromPath(string dir, string[] likeString)
        {
            path p = new path();
            foreach (DirectoryInfo di in new DirectoryInfo(dir).GetDirectories())
            {
                directoy dx = new directoy(dir, di, likeString);
                p.Dirs.Add(dx.Name, dx);
            }
            foreach (FileInfo di in new DirectoryInfo(dir).GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                file dx = new file(dir, di);
                dx.CheckLike(likeString);

                p.Files.Add(dx.Name, dx);
            }
            return p;
        }
    }
}