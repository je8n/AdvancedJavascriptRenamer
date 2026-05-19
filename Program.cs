using System;
using System.IO;
using System.Windows.Forms;

namespace advancedRenamer
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                DeleteStaleErrorLog();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += (sender, e) => ShowStartupError(e.Exception);
                AppDomain.CurrentDomain.UnhandledException += (sender, e) => ShowStartupError(e.ExceptionObject as Exception);
                Application.Run(new Form1(args));
            }
            catch (Exception ex)
            {
                ShowStartupError(ex);
            }
        }

        private static void ShowStartupError(Exception exception)
        {
            string message = exception == null ? "Bilinmeyen hata." : exception.ToString();

            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "advancedRenamer-error.log");
                File.WriteAllText(logPath, message);
            }
            catch
            {
                // Last-resort UI error reporting must not fail because logging failed.
            }

            MessageBox.Show(message, "Advanced Javascript Renamer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void DeleteStaleErrorLog()
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "advancedRenamer-error.log");
                if (File.Exists(logPath))
                {
                    File.Delete(logPath);
                }
            }
            catch
            {
                // A stale log is harmless if it cannot be deleted.
            }
        }
    }
}
