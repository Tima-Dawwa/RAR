using Microsoft.Win32;
using System;
using System.IO;
using System.Windows.Forms;

namespace RAR.UI
{
    class ContextMenuInstaller
    {
        private const string AppName = "FileCompressor";
        private const string MenuText = "Compress with File Compressor";
        private const string MenuCommand = "compress";

        public static void InstallContextMenu()
        {
            try
            {
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey("*\\shell\\" + AppName))
                {
                    key.SetValue("", MenuText);
                    key.SetValue("Icon", Application.ExecutablePath);

                    using (RegistryKey commandKey = key.CreateSubKey("command"))
                    {
                        string exePath = $"\"{Application.ExecutablePath}\" \"{MenuCommand}\" \"%1\"";
                        commandKey.SetValue("", exePath);
                    }
                }

                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey("Directory\\shell\\" + AppName))
                {
                    key.SetValue("", MenuText);
                    key.SetValue("Icon", Application.ExecutablePath);

                    using (RegistryKey commandKey = key.CreateSubKey("command"))
                    {
                        string exePath = $"\"{Application.ExecutablePath}\" \"{MenuCommand}\" \"%1\"";
                        commandKey.SetValue("", exePath);
                    }
                }

                MessageBox.Show("Context menu installed successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Please run as administrator to install context menu.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error installing context menu: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void UninstallContextMenu()
        {
            try
            {
                Registry.ClassesRoot.DeleteSubKeyTree("*\\shell\\" + AppName, false);
                Registry.ClassesRoot.DeleteSubKeyTree("Directory\\shell\\" + AppName, false);

                MessageBox.Show("Context menu uninstalled successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error uninstalling context menu: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}