using System;
using System.Globalization;

namespace csSync
{
    public class LIB
    {
        /// <summary>
        /// Imprime en consola una linea con la hora actual
        /// </summary>
        /// <param name="p"></param>
        public static void WriteLine(string p) { Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "] " + p); }
        /// <summary>
        /// Devuelve la interpretación de tamaño de un double
        /// </summary>
        /// <param name="val">Valor</param>
        /// <param name="agroup">Separar los miles</param>
        /// <returns>Devuelve la interpretación de tamaño de un double</returns>
        public static string DoubleAKB(double val, bool agroup)
        {
            string moneda = "Bytes";
            if (val >= 1024) { val /= 1024; moneda = "Kb"; }
            if (val >= 1024) { val /= 1024; moneda = "Mb"; }
            if (val >= 1024) { val /= 1024; moneda = "Gb"; }
            if (val >= 1024) { val /= 1024; moneda = "Tb"; }

            return val.ToString((agroup ? "#,###,##0.00" : "########0.00") + " '" + moneda + "'", CultureInfo.InvariantCulture);
        }
    }
}