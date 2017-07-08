using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Input;
using ThumbGen;

namespace FileExplorer.Model
{
    /// <summary>
    /// Class to get file system information
    /// </summary>
    internal class FileSystemExplorerService
    {
        /// <summary>
        /// Gets the list of files in the directory Name passed
        /// </summary>
        /// <param name="directory">The Directory to get the files from</param>
        /// <returns>Returns the List of File info for this directory.
        /// Return null if an exception is raised</returns>
        public static IList<FileInfo> GetChildFiles(string directory)
        {
            Mouse.SetCursor(Cursors.Wait);
            try
            {
                try
                {
                    //return (from x in Directory.GetFiles(directory)
                    //select new FileInfo(x)).ToList();
                    return new ThumbGen.FilesCollector().CollectFiles(directory, false, false).ToList();

                }
                catch (Exception e)
                {
                    Trace.WriteLine(e.Message);
                }
            }
            finally
            {
                Mouse.SetCursor(Cursors.Arrow);
            }
            return new List<FileInfo>();
        }


        /// <summary>
        /// Gets the list of directories 
        /// </summary>
        /// <param name="directory">The Directory to get the files from</param>
        /// <returns>Returns the List of directories info for this directory.
        /// Return null if an exception is raised</returns>
        public static IList<DirectoryInfo> GetChildDirectories(string directory)
        {

            try
            {
                try
                {
                    return new ThumbGen.FilesCollector().CollectFolders(directory, false, false).ToList();
                }
                catch (Exception e)
                {
                    Loggy.Logger.DebugException("Cannot get child dirs for " + directory, e);
                }
            }
            finally
            {

            }
            return new List<DirectoryInfo>();
        }

        /// <summary>
        /// Gets the root directories of the system
        /// </summary>
        /// <returns>Return the list of root directories</returns>
        public static IList<DriveInfo> GetRootDirectories()
        {
            return (from x in DriveInfo.GetDrives() select x).ToList();
        }
    }
}
