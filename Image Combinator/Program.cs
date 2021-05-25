using Image_Combinator.Utils;
using System;
using System.Windows.Forms;

namespace Image_Combinator
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
            //FtpOperation ftpOperation = new FtpOperation();
            //ftpOperation.FtpInfo();
            //Console.WriteLine();
        }
    }
}
