using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using CommandLine;
using CommandLine.Text;
using System.Diagnostics;
namespace Sha1Sum
{
    // <author>Jeff Anderson https://jeff.forsale</author>
    // <date>October 13, 2016 1:29:58 PM </date>
    // <summary>SHA1 Hash utitlity</summary>
    class Program
    {
        //Using CommandLine from https://commandline.codeplex.com/
        class Options
        {
            [Option('p', "path", Required = true, HelpText = "Input file or directory to be hashed.")]
            public string workingObj { get; set; }
            [Option('d',"debug", Required = false, HelpText = "Enable debug output to console window.")]
            public bool bDebug { get; set; }
            [Option('r', "recursive", Required = false, HelpText = "Recursively hash a directory, true or false, false by default")]
            public bool bRecursive { get; set; }
            [Option('e', "exclude", Required = false, HelpText = "Comma seperated list of file extension to ignore.  .exe,.log,.txt")]
            public string strExcludeExtension { get; set; }
            [Option('s', "stripquotes", Required = false, HelpText = "Strip single and double quotes from parameter value")]
            public bool bStripQuotes { get; set; }
            [ParserState]
            public IParserState LastParserState { get; set; }
            [HelpOption]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            }
        }
        //Setup static working variables
        private static string @workingObj;
        private static bool bDebug;        
        private static bool bRecursive;
        private static int iAccessDenied;
        private static int iObjCount;
        private static string strExcludeExtension;
        private static bool bStripQuotes;
        private static List<string> filterExtension;
        private static int iFilesFiltered = 0;
        static void Main(string[] args)
        {
            //Start a new timer.  If debug is enabled, the elapsed time will be displayed
            var timer = Stopwatch.StartNew();
            //Set access denied counter to 0
            iAccessDenied = 0;
            //Set static working variables based on command line args
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {                
                workingObj = (options.bStripQuotes) ? options.workingObj.Replace("\"", string.Empty).Replace("'", string.Empty) : options.workingObj;                
                bDebug = options.bDebug;
                bRecursive = (options.bRecursive == true);
                strExcludeExtension = options.strExcludeExtension;
                debug(workingObj);
            }            
            //Check if the working object exists
            if (objExists(workingObj))
            {
                Console.WriteLine(Sha1Hash(workingObj));
                debug("SHA1 Checksum complete");
            }
            //Working object does not exists.  Return -1
            else
            {
                debug("Working object does not exist.  If this is a directory, please leave off the trailing slash or use double \\\\");
                Console.WriteLine(-1);
            }
            //Stop the timer and echo out the data
            timer.Stop();
            debug("Processed " + iObjCount + " file(s)");
            if (iFilesFiltered > 0)
                debug("Excluded " + iFilesFiltered + " file(s)");
            if (iAccessDenied > 0)
                debug("Denied access on " + iAccessDenied + " file(s)");
            debug("Execution Time: " + timerDisplay(timer.ElapsedMilliseconds));    
        }
        //Quick and dirty way to convert milliseconds to seconds/minutes
        //This is only called if debug is enabled
        private static string timerDisplay(double ms)
        {
            if (ms < 1000) //Return MS
            {
                return ms + "ms";
            }
            else if (ms >= 1000 && ms < 60000) {  //Use seconds 
                return Convert.ToString(TimeSpan.FromMilliseconds(ms).TotalSeconds) + "s";
            } else //Return minutes
            {
                return Convert.ToString(TimeSpan.FromMilliseconds(ms).TotalMinutes) + "m";
            }
        }
        //Checks if an object exists
        private static bool objExists(string objPath)
        {
            try {
                if (isDirectory(objPath))
                {
                    debug("Object is a directory");
                    return true;
                }
                else
                {
                    if (File.Exists(objPath))
                    {
                        debug("Object is a file.  Filters will be removed");
                        return true;
                    } else
                    {
                        debug("Object does not exist");
                        return false;
                    }
                }
            }
            catch
            {
                debug("Fell through try catch on objExists method");
                return false;
            }
        }
        //Check if object is directory
        private static bool isDirectory(string objPath)
        {
            try
            {
                bool isDir = (File.GetAttributes(@objPath) & FileAttributes.Directory) == FileAttributes.Directory;
                return isDir;
            } catch
            {
                return false;
            }
        }

        //New Sha1 hash
        private static string Sha1Hash(string objPath)
        {
            bool tmpIsDirectory = isDirectory(objPath);
            bool fileExtensionFilter = false;
            string fileHash = null;
            //Check if 1-exclude flag was set and contains paths
            if (!String.IsNullOrEmpty(strExcludeExtension) && tmpIsDirectory)
            {
                //Set fileExtensionFilter to true so we don't run code if there are no filters
                fileExtensionFilter = true;
                //Create a blank list to hold the filtered file extensions
                filterExtension = new List<string>();
                //Check if multiple extensions were specified
                if (strExcludeExtension.Contains(','))
                {
                    //Multiple extensions were provided, expand to array
                    foreach (string tmpExt in strExcludeExtension.Split(','))
                    {
                        //Check if the user added a . before the extension, if not, add it
                        if (tmpExt.Substring(0, 1) != ".")
                        {
                            filterExtension.Add("." + tmpExt);
                            debug("Added new filter: ." + tmpExt);
                        }
                        else
                        {
                            filterExtension.Add(tmpExt);
                            debug("Added new filter: " + tmpExt);
                        }
                    }
                }
                else //Single file extension provided
                {
                    //Check if the user added a . before the extension, if not, add it
                    if (strExcludeExtension.Substring(0, 1) != ".")
                    {
                        filterExtension.Add("." + strExcludeExtension);
                        debug("Added new filter: ." + strExcludeExtension);
                    }
                    else
                    {
                        filterExtension.Add(strExcludeExtension);
                        debug("Added new filter: " + strExcludeExtension);
                    }
                }
            }
            //Create the sha1 instance
            //SHA1 sha1 = SHA1.Create();
            //Get a list of subdirectories and files
            IList<string> files = new List<string>();
            files = GetFiles(objPath, files);
            //Ensure there are files to work with
            if (files.Count > 0 && files != null)
            {
                try
                {
                    //Loop through the files
                    for (int i = 0; i < files.Count; i++)
                    {
                        string tmpFileExt = Path.GetExtension(files[i]);
                        bool filterMatch = false;

                        //Check if we are filtering files by extension
                        //No need to pull the extension if we are not filtering
                        //Also, we will not apply a file filter if the user specified a file instead of a directory
                        if (fileExtensionFilter && tmpIsDirectory)
                        {
                            //Ensure that the filterExtension list contains an item
                            if (filterExtension.Count > 0)
                            {
                                foreach (string tmpFilterExtension in filterExtension)
                                {
                                    if (tmpFileExt.ToLower().Trim() == tmpFilterExtension.ToLower().Trim())
                                    {
                                        iFilesFiltered++;
                                        filterMatch = true;
                                    }
                                }

                            }
                        }
                        if (filterMatch)
                            continue;
                        //Add to the file counter
                        iObjCount++;                        
                        try
                        {
                            string file = files[i];
                            debug(file);
                            using (FileStream fs = new FileStream(@file, FileMode.Open))
                            using (BufferedStream bs = new BufferedStream(fs))
                            {
                                using (SHA1Managed sha1hash = new SHA1Managed())
                                {
                                    byte[] hash = sha1hash.ComputeHash(bs);
                                    StringBuilder formatted = new StringBuilder(2 * hash.Length);
                                    foreach (byte b in hash)
                                    {
                                        formatted.AppendFormat("{0:x2}", b);
                                    }
                                    fileHash = formatted.ToString();
                                }
                            }
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            fileHash = "Access Denied";
                        }
                    }
                }
                catch (Exception ex)
                {
                    debug("Directory may be empty: " + ex.ToString());
                    fileHash = "Error";
                }
            }
            return fileHash;
        }

        //Perform the Sha1 hashing
        private static string Sha1(string objPath)
        {
            bool tmpIsDirectory = isDirectory(objPath);
            bool fileExtensionFilter = false;
            //Check if 1-exclude flag was set and contains paths
            if (!String.IsNullOrEmpty(strExcludeExtension) && tmpIsDirectory)
            {
                //Set fileExtensionFilter to true so we don't run code if there are no filters
                fileExtensionFilter = true;
                //Create a blank list to hold the filtered file extensions
                filterExtension = new List<string>();
                //Check if multiple extensions were specified
                if (strExcludeExtension.Contains(','))
                {
                    //Multiple extensions were provided, expand to array
                    foreach(string tmpExt in strExcludeExtension.Split(','))
                    {
                        //Check if the user added a . before the extension, if not, add it
                        if (tmpExt.Substring(0, 1) != ".")
                        {
                            filterExtension.Add("." + tmpExt);
                            debug("Added new filter: ." + tmpExt);
                        }
                        else
                        {
                            filterExtension.Add(tmpExt);
                            debug("Added new filter: " + tmpExt);
                        }
                    }
                }
                else //Single file extension provided
                {
                    //Check if the user added a . before the extension, if not, add it
                    if (strExcludeExtension.Substring(0, 1) != ".")
                    {
                        filterExtension.Add("." + strExcludeExtension);
                        debug("Added new filter: ." + strExcludeExtension);
                    }
                    else
                    {
                        filterExtension.Add(strExcludeExtension);
                        debug("Added new filter: " + strExcludeExtension);
                    }
                }
            }
            //Create the sha1 instance
            SHA1 sha1 = SHA1.Create();
            //Get a list of subdirectories and files
            IList<string> files = new List<string>();
            files = GetFiles(objPath, files);
            //Ensure there are files to work with
            if (files.Count > 0 && files != null)
            {
                try {
                    //Loop through the files
                    for (int i = 0; i < files.Count; i++)
                    {
                        string tmpFileExt = Path.GetExtension(files[i]);
                        bool filterMatch = false;
                        
                        //Check if we are filtering files by extension
                        //No need to pull the extension if we are not filtering
                        //Also, we will not apply a file filter if the user specified a file instead of a directory
                        if (fileExtensionFilter && tmpIsDirectory)
                        {                            
                            //Ensure that the filterExtension list contains an item
                            if (filterExtension.Count > 0)
                            {
                                foreach (string tmpFilterExtension in filterExtension)
                                {
                                    if (tmpFileExt.ToLower().Trim() == tmpFilterExtension.ToLower().Trim())
                                    {
                                        iFilesFiltered++;
                                        filterMatch = true;
                                    }
                                }
                                                          
                            }               
                        }
                        if (filterMatch)
                            continue;
                        //Add to the file counter
                        iObjCount++;
                        try
                        {                            
                            string file = files[i];
                            debug(file);
                            string relativePath = file.Substring(objPath.Length + 1);
                            byte[] pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
                            sha1.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                            byte[] contentBytes = File.ReadAllBytes(file);
                            if (i == files.Count - 1)
                                sha1.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
                            else
                                sha1.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);

                        }
                        catch (Exception ex)
                        {
                            //Access denied, add to access denied counter
                            iAccessDenied++;
                        }
                    }
                } catch (Exception ex)
                {
                    debug("Directory may be empty");
                }
            }
            string leftHash;
            //If the provided path was a directory, recursive was disabled and the directory is empty, we will hit an error here
            try
            {
                leftHash = BitConverter.ToString(sha1.Hash).Replace("-", "").ToLower();
            }
            catch
            {
                //Directory does not contain any files and the recursive flag was turned off
                //Create a hash based on directory name+last modified time 
                //Does not check last modified time of child folders.  So a new file in a sub folder will not change the hash.
                //Recommend using the -r flag on directories without files
                string lastWrite = Directory.GetLastWriteTime(objPath).ToString();
                debug(lastWrite);
                byte[] nameBytes = Encoding.ASCII.GetBytes(objPath + "-" + lastWrite);
                byte[] hash = sha1.ComputeHash(nameBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                leftHash = sb.ToString();
            }

            return leftHash;
            
        }
        //Safe way to recursively search a directory and avoid access denied errors
        private static IList<string> GetFiles(string parent, IList<string> files)
        {            
            try
            {
                Directory.GetFiles(parent).ToList().ForEach(s => files.Add(s));
                if (bRecursive)
                    Directory.GetDirectories(parent).ToList().ForEach(s => GetFiles(s, files));                   
            } catch(UnauthorizedAccessException ex)
            {
                iAccessDenied++;
            } catch
            {
                //Usually indicates this is a file and not a directory
                files.Add(parent);
            }
            return files;
        }
        //Debug strings
        private static void debug(string str)
        {
            if(bDebug)
                Console.WriteLine(str);
        }
    }
}
