using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace advancedRenamer
{
    public static class RegistryHelper
    {
        private const string MenuText = "Open with Advanced Javascript Renamer";
        private const string DirectoryShellKey = @"Software\Classes\Directory\shell\advancedRenamer";
        private const string BackgroundShellKey = @"Software\Classes\Directory\Background\shell\advancedRenamer";

        public static bool IsContextMenuInstalled()
        {
            return KeyExists(DirectoryShellKey) && KeyExists(BackgroundShellKey);
        }

        public static void InstallContextMenu()
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                throw new InvalidOperationException("Advanced Javascript Renamer executable yolu bulunamadı.");
            }

            WriteMenuKey(DirectoryShellKey, exePath, "\"%1\"");
            WriteMenuKey(BackgroundShellKey, exePath, "\"%V\"");
        }

        public static void RemoveContextMenu()
        {
            DeleteKeyTree(DirectoryShellKey);
            DeleteKeyTree(BackgroundShellKey);
        }

        private static bool KeyExists(string keyPath)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath))
            {
                return key != null;
            }
        }

        private static void WriteMenuKey(string shellKeyPath, string exePath, string argumentToken)
        {
            using (RegistryKey shellKey = Registry.CurrentUser.CreateSubKey(shellKeyPath))
            using (RegistryKey commandKey = Registry.CurrentUser.CreateSubKey(shellKeyPath + @"\command"))
            {
                if (shellKey == null || commandKey == null)
                {
                    throw new InvalidOperationException("Registry anahtarı oluşturulamadı.");
                }

                shellKey.SetValue(null, MenuText);
                shellKey.SetValue("Icon", exePath);
                commandKey.SetValue(null, "\"" + exePath + "\" " + argumentToken);
            }
        }

        private static void DeleteKeyTree(string keyPath)
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(keyPath, false);
            }
            catch (ArgumentException)
            {
                // Key already does not exist.
            }
        }
    }
}
