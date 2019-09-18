using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Diagnostics;
using iText.Kernel.Pdf;
using iText.Forms;
using iText.Forms.Fields;

namespace iPipelineCSDHelperTool
{
    class Program
    {
        static void Main(string[] args)
        {
            if (File.Exists("Reference.xml"))
            {
                String referenceFilePath = @"Reference.xml";
                XmlDocument document = new XmlDocument();
                document.Load(referenceFilePath);
                String csdFilePath = String.Empty;
                String configFilePath = String.Empty;
                String projectFolderPath = String.Empty;
                String objectLogFilePath = String.Empty;
                Dictionary<String, String> dictionary = new Dictionary<string, string>();
                if (document.SelectSingleNode("//CsdFilePath") != null)
                {
                    csdFilePath = document.SelectSingleNode("//CsdFilePath").InnerText;
                }
                else
                {
                    throw new Exception("No node named 'CsdFilePath' found in the reference file");
                }
                if(document.SelectSingleNode("//ConfigFilePath") != null)
                {
                    configFilePath = document.SelectSingleNode("//ConfigFilePath").InnerText;
                }
                else
                {
                    throw new Exception("No node named 'ConfigFilePath' found in the reference file");
                }
                if(document.SelectSingleNode("//ProjectPath") != null)
                {
                    projectFolderPath = document.SelectSingleNode("//ProjectPath").InnerText;
                }
                else
                {
                    throw new Exception("No node named 'ProjectPath' found in the reference file");
                }
                if(document.SelectSingleNode("//LogObjectListPath") != null)
                {
                    objectLogFilePath = document.SelectSingleNode("//LogObjectListPath").InnerText;
                }
                else
                {
                    throw new Exception("No node named 'LogObjectListPath' found in the reference file");
                }
                
                String objectType = String.Empty;
                String source = String.Empty;
                String target = String.Empty;
                String fileTypes = String.Empty;
                Helper.Helper helper = null;
                if (document.SelectSingleNode("//Objects").HasChildNodes)
                {
                    foreach(XmlNode node in document.SelectNodes("//Object"))
                    {
                        if (node.Attributes["Type"] != null)
                        {
                            if (node.HasChildNodes)
                            {
                                foreach (XmlNode childNode in node.ChildNodes)
                                {
                                    if (childNode.Name.Equals("Source"))
                                    {
                                        source = childNode.InnerText;
                                    }
                                    else if (childNode.Name.Equals("Target"))
                                    {
                                        target = childNode.InnerText;
                                    }
                                    else if (childNode.Name.Equals("FileTypes"))
                                    {
                                        fileTypes = childNode.InnerText;
                                    }
                                }
                            }
                            else
                            {
                                continue;
                            }
                            objectType = node.Attributes["Type"].Value;

                            switch (objectType)
                            {
                                case "CheckBox":                                   
                                    Console.WriteLine(String.Format("Object Type - '{0}' is being modified from {1} to {2} in files {3}",objectType,source,target,fileTypes));
                                    helper = new Helper.Helper(csdFilePath, configFilePath, objectLogFilePath);
                                    dictionary = helper.SearchObjects(objectType);
                                    ConvertFiles(projectFolderPath, fileTypes, dictionary,source,target);
                                    helper = null;
                                    break;
                                case "DateTime":
                                    Console.WriteLine("");
                                    helper = new Helper.Helper(csdFilePath, configFilePath, objectLogFilePath);
                                    dictionary = helper.SearchObjects(objectType);
                                    ConvertFiles(projectFolderPath,fileTypes,dictionary,source,target);
                                    helper = null;
                                    break;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Object Type is not defined");
                        }
                    }
                }
            }
            else
            {
                throw new Exception("Reference File Not found or read properly from root directory");
            }            
        }

        public static void ConvertFiles(String projectPath, String fileExtensions, Dictionary<String,String> dictionary, String source, String target)
        {
            String[] files;
            String[] fileTypes = fileExtensions.Split('|');
            String backup = CreateBackup(projectPath);
            foreach (String fileExtension in fileTypes)
            {
                files = Directory.GetFiles(projectPath, fileExtension, SearchOption.AllDirectories);
                
                switch (fileExtension){
                    case "*.xsl":
                        XSLFileContentSearch(files, dictionary, source, target,projectPath,backup);
                        break;
                    case "*.vb":
                        VBFileContentSearch(files, dictionary, source, target, projectPath, backup);
                        break;
                    case "*.pdf":
                        PDFContentSearch(files, dictionary, source, target, projectPath, backup);
                        break;
                }
            }
        }

        private static void PDFContentSearch(string[] files, Dictionary<string, string> dictionary, string source, string target, string projectPath, string backup)
        {
            foreach (String file in files)
            {
                Dictionary<String, String> dic = new Dictionary<String, string>();
                String newFilePath = file.Replace(projectPath, "");
                String newDirectoryName = System.IO.Path.GetDirectoryName(newFilePath);
                if (!Directory.Exists(backup + newDirectoryName) && !newDirectoryName.Equals("\\"))
                {
                    Directory.CreateDirectory(backup + newDirectoryName);
                }
                String targetPath = backup + newFilePath;
                try
                {
                    System.IO.File.Copy(file, targetPath, true);                  
                    iText.Kernel.Pdf.PdfReader pdfReader = new PdfReader(file);
                    PdfWriter pdfWriter = new PdfWriter(targetPath);
                    PdfDocument pdfDocument = new PdfDocument(pdfReader,pdfWriter);
                    PdfAcroForm pdfAcroForm = PdfAcroForm.GetAcroForm(pdfDocument, false);                   
                    IDictionary<String, PdfFormField> field = pdfAcroForm.GetFormFields();
                    foreach (String key in pdfAcroForm.GetFormFields().Keys)
                    {
                        
                        if(key.Contains("DOB") || key.Contains("Date"))
                        {
                            if (!key.EndsWith(target))
                            {
                                dic.Add(key, key + target);
                            }
                        }
                    }
                    foreach(String field1 in dic.Keys)
                    {
                        pdfAcroForm.RenameField(field1, dic[field1]);
                    }
                    
                    pdfDocument.Close();
                    

                }
                catch(Exception ex)
                {
                    Console.WriteLine("Exception occurred :" + ex);
                    continue;
                }
            }
        }

        private static void XSLFileContentSearch(String[] files, Dictionary<String, String> dictionary, String source, String target,string projectPath,String backup)
        {
            foreach (String file in files)
            {
                
                Dictionary<int, String> dic = new Dictionary<int, string>();
                String newFilePath = file.Replace(projectPath, "");
                String newDirectoryName = System.IO.Path.GetDirectoryName(newFilePath);
                if (!Directory.Exists(backup + newDirectoryName) && !newDirectoryName.Equals("\\"))
                {
                    Directory.CreateDirectory(backup + newDirectoryName);
                }
                String targetPath = backup + newFilePath;
                StreamWriter writer = new StreamWriter(targetPath);
                int count = 0;
                foreach (String line in File.ReadLines(file))
                {
                    foreach (String key in dictionary.Keys)
                    {
                        if (line.Contains(key) && line.Contains(source) && !dic.ContainsKey(count))
                        {
                            dic.Add(count, line.Replace(source, target));
                        }
                    }
                    if (dic.ContainsKey(count))
                    {
                        writer.WriteLine(dic[count]);
                    }
                    else
                    {
                        writer.WriteLine(line);
                    }
                    count += 1;
                }
                writer.Flush();
                writer.Close();
                if(dic.Count > 0)
                {
                    OpenBeyondCompare(file, targetPath);
                }
            }
        }

        private static void VBFileContentSearch(String[]files, Dictionary<String, String> dictionary, String source, String target,String projectpath, String backup)
        {
            foreach (String file in files)
            {
                Dictionary<int, String> dic = new Dictionary<int, string>();
                String newFilePath = file.Replace(projectpath, "");
                String newDirectoryName = System.IO.Path.GetDirectoryName(newFilePath);
                if(!Directory.Exists(backup + newDirectoryName) && !newDirectoryName.Equals("\\"))
                {
                    Directory.CreateDirectory(backup + newDirectoryName);
                }
                String targetPath = backup + newFilePath;
                StreamWriter writer = new StreamWriter(targetPath);
                int count = 0;
                foreach (String line in File.ReadLines(file))
                {
                    foreach (String key in dictionary.Keys)
                    {
                        if (line.Contains(key) && line.Contains(source) && !dic.ContainsKey(count))
                        {
                            
                            dic.Add(count, line.Replace(source,target));
                        }
                        else if (line.Contains(key.Substring(3)) && line.Contains(source) && !dic.ContainsKey(count))
                        {
                            dic.Add(count, line.Replace(source, target));
                        }
                    }
                    if (dic.ContainsKey(count))
                    {
                        writer.WriteLine(dic[count]);
                    }
                    else
                    {
                        writer.WriteLine(line);
                    }
                    count += 1;
                }
                writer.Flush();
                writer.Close();
                if(dic.Count > 0)
                {
                    OpenBeyondCompare(file, targetPath);
                }
            }
        }

        private static String CreateBackup(String projectPath)
        {
            String dateTime = DateTime.Now.ToString().Replace("/", "-").Replace(" ", "_").Replace(":", "-");
            String backup = projectPath + "_Converted" + dateTime;
            if (!Directory.Exists(backup))
            {
                Directory.CreateDirectory(backup);
            }
            return backup;
        }
        
        private static void OpenBeyondCompare(String sourceFile, String targetFile)
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();            
            startInfo.FileName = "BCompare.exe";
            startInfo.Arguments = @"/c " + sourceFile+" " + targetFile;
            Process.Start(startInfo);
        } 
    }
}
