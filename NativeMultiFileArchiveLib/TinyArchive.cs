using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

#nullable disable

// ReSharper disable EventNeverSubscribedTo.Global

namespace NativeMultiFileArchiveLib
{
    /// <summary>
    ///     a class that can be serialized and compressed to disk, that contains a list of files and their
    ///     contents.
    ///     the entire archive file must be loaded into memory in order to work
    ///     so this is only appropriate for a small number of relatively small files.
    /// </summary>
    [Serializable]
    public class TinyFileArchive
    {
        #region Fields:

        protected Dictionary<string, List<ArchiveFile>> fileSystem =
            new Dictionary<string, List<ArchiveFile>>();

        #endregion

        #region Constructor

        public TinyFileArchive()
        {
            // create the root folder:
            this.CreateFolder("Archive");
        }

        #endregion

        /// <summary>
        ///     tests the process.
        /// </summary>
        public static void Test()
        {
            var reloaded = Open(@"C:\Temp\test.arc");

            var hierarchy = reloaded.CreateFolderTree('\\');
            foreach (var node in hierarchy.GetAllNodesInHierarchyOrder())
            {
                var path = node.Value;
                foreach (var file in reloaded.GetDirectoryFiles(path))
                {
                    Debug.Print(path + "\\" + file.Name);
                }
            }
        }

        #region Events

        /// <summary>
        ///     raised when a file is added to the archive.
        /// </summary>
        public event EventHandler<FileEventArgs> FileAdded;

        /// <summary>
        ///     raised when a file is updated in the archive.
        /// </summary>
        public event EventHandler<FileEventArgs> FileUpdated;

        /// <summary>
        ///     raised when a file is removed from the archive.
        /// </summary>
        public event EventHandler<FileEventArgs> FileDeleted;

        /// <summary>
        ///     raised when a folder is added to the archive.
        /// </summary>
        public event EventHandler<DirEventArgs> DirectoryAdded;

        /// <summary>
        ///     raised when a folder is removed from the archive.
        /// </summary>
        public event EventHandler<DirEventArgs> DirectoryRemoved;

        /// <summary>
        ///     raises the FileAdded event.
        /// </summary>
        /// <param name="file"></param>
        protected virtual void OnFileAdded(ArchiveFile file)
        {
            this.FileAdded?.Invoke(
                this,
                new FileEventArgs
                    {File = file});
        }

        /// <summary>
        ///     raises the FileUpdated event.
        /// </summary>
        /// <param name="file"></param>
        protected virtual void OnFileUpdated(ArchiveFile file)
        {
            this.FileUpdated?.Invoke(
                this,
                new FileEventArgs
                    {File = file});
        }

        /// <summary>
        ///     raises the FileDeleted event.
        /// </summary>
        /// <param name="file"></param>
        protected virtual void OnFileDeleted(ArchiveFile file)
        {
            this.FileDeleted?.Invoke(
                this,
                new FileEventArgs
                    {File = file});
        }

        /// <summary>
        ///     raises the DirectoryAdded event.
        /// </summary>
        /// <param name="dirName"></param>
        protected virtual void OnDirAdded(string dirName)
        {
            this.DirectoryAdded?.Invoke(
                this,
                new DirEventArgs
                    {DirectoryName = dirName});
        }

        /// <summary>
        ///     raises the DirectoryRemoved event.
        /// </summary>
        /// <param name="dirName"></param>
        protected virtual void OnDirRemoved(string dirName)
        {
            this.DirectoryRemoved?.Invoke(
                this,
                new DirEventArgs
                    {DirectoryName = dirName});
        }

        #endregion

        #region Load/Save

        public void Save()
        {
            if (!string.IsNullOrEmpty(this.FileName.Trim()))
            {
                this.SaveAs(this.FileName);
            }
            else
            {
                throw new ApplicationException("File Name Not Specified");
            }
        }

        /// <summary>
        ///     save the archive file.
        /// </summary>
        /// <param name="archiveFileName"></param>
        public void SaveAs(string archiveFileName)
        {
            // update the filename
            this.FileName = archiveFileName;

            // save as:
            using var fs = File.Create(archiveFileName);
            TinySerializer.Serialize(fs, this);
        }

        /// <summary>
        ///     open an existing archive file.
        /// </summary>
        /// <param name="archiveFileName"></param>
        /// <returns></returns>
        public static TinyFileArchive Open(string archiveFileName)
        {
            using var fs = File.Open(archiveFileName, FileMode.Open);
            return TinySerializer.DeSerialize<TinyFileArchive>(fs);
        }

        #endregion

        #region File Retrieval Methods

        /// <summary>
        ///     gets a list of all file paths. (directoryname \ filename)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetFileNames()
        {
            foreach (var path in fileSystem.Keys)
            {
                foreach (var file in fileSystem[path])
                {
                    yield return path + "\\" + file.Name;
                }
            }
        }

        /// <summary>
        ///     gets a list of all the paths (directories) in the system
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetDirectories()
        {
            return from path in fileSystem.Keys select path;
        }

        /// <summary>
        ///     gets a list of all the files in the system.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ArchiveFile> GetFiles()
        {
            foreach (var path in fileSystem.Keys)
            {
                foreach (var file in fileSystem[path])
                {
                    yield return file;
                }
            }
        }

        /// <summary>
        ///     gets a list of all the files in a specific directory.
        /// </summary>
        /// <param name="dirName"></param>
        /// <returns></returns>
        public List<ArchiveFile> GetDirectoryFiles(string dirName)
        {
            if (fileSystem.ContainsKey(dirName))
            {
                return fileSystem[dirName];
            }

            return new List<ArchiveFile>();
        }

        /// <summary>
        ///     locate a file by it's full name within the archive.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public ArchiveFile GetFileByName(string fileName)
        {
            // break the "filename" into a path and name:
            var path = Path.GetDirectoryName(fileName)!;
            var name = Path.GetFileName(fileName);

            // locate the folder:
            if (fileSystem.ContainsKey(path!))
            {
                // search each item in the folder:
                foreach (var file in fileSystem[path])
                    // yield the file that matches:
                {
                    if (file.Name.Equals(name))
                    {
                        return file;
                    }
                }
            }

            // couldn't find the file:
            throw new ArgumentException("File Not Found:" + fileName);
        }

        /// <summary>
        ///     return a list of files based on the original filename.
        /// </summary>
        /// <param name="originalFileName"></param>
        /// <returns></returns>
        public IEnumerable<ArchiveFile> GetFilesByOriginalFileName(string originalFileName)
        {
            return from file in this.GetFiles()
                where file.OriginalFileName.Equals(
                    originalFileName,
                    StringComparison.OrdinalIgnoreCase)
                select file;
        }

        #endregion

        #region File Search

        /// <summary>
        ///     search for files matching the specified regular expression. match is done on directory name as
        ///     well as filename
        /// </summary>
        /// <param name="regex"></param>
        /// <returns></returns>
        public IEnumerable<ArchiveFile> Dir(string regex)
        {
            return from file in this.GetFiles()
                where Regex.IsMatch(file.Path + "\\" + file.Name, regex)
                select file;
        }

        /// <summary>
        ///     search for files matching the specified regular expression, within the specified folder.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="regex"></param>
        /// <returns></returns>
        public IEnumerable<ArchiveFile> Dir(string folder, string regex)
        {
            return from file in this.GetDirectoryFiles(folder)
                where Regex.IsMatch(file.Name, regex)
                select file;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     gets or sets a file by it's file-name.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public ArchiveFile this[string fileName]
        {
            get =>
                // find and return the file or throw a file-not-found exception.
                this.GetFileByName(fileName);
            set =>
                // add the file
                this.AddFile(fileName, value);
        }

        public string FileName { get; set; }

        public int DirectoryCount => fileSystem.Keys.Count;

        public int FileCount => this.GetFiles().Count();

        #endregion

        #region Add/Remove

        /// <summary>
        ///     add the file using it's directory and name.
        /// </summary>
        /// <param name="value"></param>
        public void AddFile(ArchiveFile value)
        {
            this.AddFile(value.Path + "\\" + value.Name, value);
        }

        /// <summary>
        ///     add the archive file into the specific directory with the specific name.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="value"></param>
        public void AddFile(string fileName, ArchiveFile value)
        {
            // extract the path and name of the file.
            var path = Path.GetDirectoryName(fileName)!;


            var name = Path.GetFileName(fileName);

            // the name and path and owner are modified to match:
            value.Path = path!;
            value.Name = name;


            // does the path already exist?
            if (!fileSystem.ContainsKey(path))
            {
                // create the folder:
                fileSystem.Add(path, new List<ArchiveFile>());

                // raise the event;
                this.OnDirAdded(path);
            }

            // does the folder already contain the file?
            if (fileSystem[path].Contains(value))
            {
                // yes.. get the index of the file:
                var i = fileSystem[path].IndexOf(value);

                // set the specific index to the input value:
                fileSystem[path][i] = value;

                // now raise the file-updated event.
                this.OnFileUpdated(value);
            }
            else
            {
                // add the file.
                fileSystem[path].Add(value);

                // raise the file added event:
                this.OnFileAdded(value);
            }
        }

        /// <summary>
        ///     add a file from disk into the specified folder of the archive.
        /// </summary>
        /// <param name="sourceFileName">the source file path</param>
        /// <param name="destDir">the target directory in the archive.</param>
        /// <returns>the archive-file details.</returns>
        public ArchiveFile AddExistingFile(string sourceFileName, string destDir)
        {
            // create the archive file object:
            var file = new ArchiveFile(sourceFileName);

            // create the archive path:
            var path = destDir + "\\" + file.Name;

            // add into this
            this[path] = file;

            // return the file.
            return file;
        }

        /// <summary>
        ///     adds all the files in the specified source folder that match the file-spec to the destination
        ///     folder of the archive.
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="searchString"></param>
        /// <param name="destinationFolder"></param>
        /// <returns></returns>
        public int AddExistingFolder(
            string sourcePath, string searchString, string destinationFolder)
        {
            // get the matching filenames:
            var files = Directory.GetFiles(sourcePath, searchString);

            // add them in:
            foreach (var file in files)
            {
                this.AddExistingFile(file, destinationFolder);
            }

            // return the number of added files.
            return files.Length;
        }

        public void CreateFolder(string path)
        {
            if (!fileSystem.ContainsKey(path))
            {
                fileSystem.Add(path, new List<ArchiveFile>());
                this.OnDirAdded(path);
            }
        }

        /// <summary>
        ///     remove an existing file by name. returns true if the file was found and removed.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool RemoveFile(string fileName)
        {
            var path = Path.GetDirectoryName(fileName);
            var name = Path.GetFileName(fileName);

            if (fileSystem.ContainsKey(path!))
            {
                foreach (var file in fileSystem[path])
                {
                    if (file.Name.Equals(name))
                    {
                        // remove the file from the list:
                        fileSystem[path].Remove(file);

                        // raise the removed event.
                        this.OnFileDeleted(file);

                        // return with success:
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     remove all the files that match the search expression.
        /// </summary>
        /// <param name="searchExpr"></param>
        /// <returns></returns>
        public int RemoveFiles(string searchExpr)
        {
            var removed = 0;

            // enumerate the files that match the expression:
            foreach (var file in this.Dir(searchExpr))
            {
                // remove each file. increment the count:
                if (this.RemoveFile(file.Path + "\\" + file.Name))
                {
                    removed++;
                }
            }

            // return the number of files removed.
            return removed;
        }

        /// <summary>
        ///     remove the specified folder.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool RemoveFolder(string path)
        {
            if (fileSystem.ContainsKey(path))
            {
                // raise the file deleted event for each file in the folder
                foreach (var file in fileSystem[path])
                {
                    this.OnFileDeleted(file);
                }

                // remove the folder
                fileSystem.Remove(path);

                // raise the directory removed event:
                this.OnDirRemoved(path);

                // return true indicating success.
                return true;
            }

            // return false... folder not found.
            return false;
        }

        #endregion

        #region Hierarchy

        /// <summary>
        ///     returns an enumerable of folders under the specified path. if nested is true, all the
        ///     descendent folders will be listed.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="nested">if true, return all descendents of the specified path.</param>
        /// <returns></returns>
        public IEnumerable<string> GetSubFolders(string path, bool nested)
        {
            // create the folder tree:
            var folderTree = this.CreateFolderTree('\\');

            // check that the path exists:
            if (folderTree.Contains(path))
            {
                // yield all descendents:
                if (nested)
                {
                    foreach (var node in folderTree[path]?.Descendents!)
                    {
                        yield return node.Value;
                    }
                }
                else
                    // yield children only.
                {
                    foreach (var node in folderTree[path]?.Children!)
                    {
                        yield return node.Value;
                    }
                }
            }
        }

        /// <summary>
        ///     gets a list of directories out of the archive - includes "calculated" directories.
        ///     ie, if the path: Temp\List\Files\Mine exists within the dictionary, (as only 1 entry)
        ///     this will render the following list:
        ///     Temp
        ///     Temp\List
        ///     Temp\List\Files
        ///     Temp\List\Files\Mine
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetAllPaths()
        {
            foreach (var node in this.CreateFolderTree('\\').GetAllNodesInHierarchyOrder())
            {
                yield return node.Value;
            }
        }

        /// <summary>
        ///     populate a tree-view with the contents of the folder.
        /// </summary>
        /// <param name="tv"></param>
        /// <param name="pathSep"></param>
        /// <returns></returns>
        public TreeView CreateFolderTreeView(TreeView tv, char pathSep)
        {
            tv.PathSeparator = pathSep.ToString();
            tv.Nodes.Clear();

            // maintain a dictionary of paths and nodes.
            var nodes = new Dictionary<string, TreeNode>();

            foreach (var dir in this.GetDirectories())
            {
                var pathElements = dir.Split(pathSep);
                var path = "";

                TreeNode parent = null;

                foreach (var pathElement in pathElements)
                {
                    if (path.Length > 0)
                    {
                        path += pathSep;
                    }

                    path += pathElement;

                    if (parent != null)
                    {
                        if (nodes.ContainsKey(path))
                        {
                            parent = nodes[path];
                        }
                        else
                        {
                            parent = parent.Nodes.Add(path, pathElement);
                            nodes.Add(path, parent);
                        }
                    }
                    else
                    {
                        if (nodes.ContainsKey(path))
                        {
                            parent = nodes[path];
                        }
                        else
                        {
                            parent = tv.Nodes.Add(path, pathElement);
                            nodes.Add(path, parent);
                        }
                    }
                }
            }

            return tv;
        }


        /// <summary>
        ///     generates a hierarchy of folder names from the archive.
        /// </summary>
        /// <returns></returns>
        public Tree<string> CreateFolderTree(char pathSeparator)
        {
            var tree = new Tree<string>();
            TreeNode<string> parent = null;

            // enumerate the actual directories:
            foreach (var dir in this.GetDirectories())
            {
                // break the dir into folder-names:
                var elements = dir.Split(pathSeparator);

                // build the path folder at a time.
                var path = "";

                // enumerate the path elements
                foreach (var pathElement in elements)
                {
                    // append to the path variable
                    if (path.Length > 0)
                    {
                        path += pathSeparator;
                    }

                    path += pathElement;


                    if (parent != null)
                    {
                        // add the current path to the hierarchy under the parent.
                        parent = !tree.Contains(path) ? tree.Add(path, parent) : tree[path];
                    }
                    else
                    {
                        // add the current path to the top of the tree.
                        parent = !tree.Contains(path) ? tree.Add(path) : tree[path];
                    }
                }
            }

            // return the populated tree.
            return tree;
        }

        #endregion
    }

    /// <summary>
    ///     event arguments for an ArchiveFile Event.
    /// </summary>
    public class FileEventArgs : EventArgs
    {
        public ArchiveFile File { get; set; }
    }

    /// <summary>
    ///     event arguments for a Directory event.
    /// </summary>
    public class DirEventArgs : EventArgs
    {
        public string DirectoryName { get; set; }
    }
}
