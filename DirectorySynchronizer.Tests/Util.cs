using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace DirectorySynchronizer.Tests
{
    class Util
    {
        public static void RemoveDirectoryWritePermission(string path)
        {
            // AccessControl works only on Windows
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var dirInfo = new DirectoryInfo(path);
            var dirSecurity = dirInfo.GetAccessControl();
            dirSecurity.AddAccessRule(new FileSystemAccessRule(
                Environment.UserName,
                FileSystemRights.Write | FileSystemRights.CreateFiles | FileSystemRights.CreateDirectories,
                AccessControlType.Deny));
            dirInfo.SetAccessControl(dirSecurity);
        }

        public static void RemoveFileWritePermission(string path)
        {
            // AccessControl works only on Windows
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var fileInfo = new FileInfo(path);
            var fileSecurity = fileInfo.GetAccessControl();
            fileSecurity.AddAccessRule(new FileSystemAccessRule(
                Environment.UserName,
                FileSystemRights.Write,
                AccessControlType.Deny));
            fileInfo.SetAccessControl(fileSecurity);
        }

        public static void RemoveDirectoryReadPermission(string path)
        {
            // AccessControl works only on Windows
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var dirInfo = new DirectoryInfo(path);
            var dirSecurity = dirInfo.GetAccessControl();
            dirSecurity.AddAccessRule(new FileSystemAccessRule(
                Environment.UserName,
                FileSystemRights.Read | FileSystemRights.ListDirectory,
                AccessControlType.Deny));
            dirInfo.SetAccessControl(dirSecurity);
        }


        public static void RestoreDirectoryWritePermission(string path)
        {
            // AccessControl works only on Windows
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var dirInfo = new DirectoryInfo(path);
            var dirSecurity = dirInfo.GetAccessControl();
            dirSecurity.RemoveAccessRule(new FileSystemAccessRule(
                Environment.UserName,
                FileSystemRights.Write | FileSystemRights.CreateFiles | FileSystemRights.CreateDirectories,
                AccessControlType.Deny));
            dirInfo.SetAccessControl(dirSecurity);
        }

        public static void RestoreFileWritePermission(string path)
        {
            // AccessControl works only on Windows
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var fileInfo = new FileInfo(path);
            var fileSecurity = fileInfo.GetAccessControl();
            fileSecurity.RemoveAccessRule(new FileSystemAccessRule(
                Environment.UserName,
                FileSystemRights.Write,
                AccessControlType.Deny));
            fileInfo.SetAccessControl(fileSecurity);
        }

        public static void RestoreDirectoryReadPermission(string path)
        {
            // AccessControl works only on Windows
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var dirInfo = new DirectoryInfo(path);
            var dirSecurity = dirInfo.GetAccessControl();
            dirSecurity.RemoveAccessRule(new FileSystemAccessRule(
                Environment.UserName,
                FileSystemRights.Read | FileSystemRights.ListDirectory,
                AccessControlType.Deny));
            dirInfo.SetAccessControl(dirSecurity);
        }
    }
}