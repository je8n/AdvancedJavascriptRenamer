using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace advancedRenamer
{
    public static class RegistryHelper
    {
        private const string DefaultMenuText = "Open with Advanced Javascript Renamer";
        private const string DirectoryShellKey = @"Software\Classes\Directory\shell\advancedRenamer";
        private const string BackgroundShellKey = @"Software\Classes\Directory\Background\shell\advancedRenamer";

        public static bool IsContextMenuInstalled()
        {
            return KeyExists(DirectoryShellKey) && KeyExists(BackgroundShellKey);
        }

        public static void InstallContextMenu(string menuText = null)
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                throw new InvalidOperationException(LanguageManager.T("ExePathNotFound"));
            }

            string safeMenuText = string.IsNullOrWhiteSpace(menuText) ? DefaultMenuText : menuText;
            WriteMenuKey(DirectoryShellKey, exePath, "\"%1\"", safeMenuText);
            WriteMenuKey(BackgroundShellKey, exePath, "\"%V\"", safeMenuText);
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

        private static void WriteMenuKey(string shellKeyPath, string exePath, string argumentToken, string menuText)
        {
            using (RegistryKey shellKey = Registry.CurrentUser.CreateSubKey(shellKeyPath))
            using (RegistryKey commandKey = Registry.CurrentUser.CreateSubKey(shellKeyPath + @"\command"))
            {
                if (shellKey == null || commandKey == null)
                {
                    throw new InvalidOperationException(LanguageManager.T("RegistryKeyCreateFailed"));
                }

                shellKey.SetValue(null, menuText);
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
