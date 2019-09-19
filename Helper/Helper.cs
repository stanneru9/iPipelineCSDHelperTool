using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Security.AccessControl;

namespace Helper
{
    public class Helper
    {
        XmlDocument csd;
        XmlDocument config;
        String objectLogFilePath;
        public Dictionary<String, List<String>> g_dictionary = new Dictionary<String, List<String>>();
        public Dictionary<String, String> g_objectList = new Dictionary<string, String>();
        public Helper(String csdFile, String configFile, String objectLogFile)
        {
            csd = new XmlDocument();
            config = new XmlDocument();
            csd.Load(csdFile);
            config.Load(configFile);
            this.objectLogFilePath = objectLogFile;
        }
        public Helper()
        {
            //Default constructor
        }
        
        public Dictionary<String,String> SearchObjects(String type)
        {
            XmlNodeList nodes;
            if (type.Equals("*"))
            {
               nodes  = csd.SelectNodes(String.Format("//Screens/Screen/Objects/Object/Properties/Property[@Name='{0}']", "Type"));
            }
            else
            {
               nodes = csd.SelectNodes(String.Format("//Screens/Screen/Objects/Object/Properties/Property[@Name='Type' and text()='{0}']", type));
            }
            
            foreach (XmlNode node in nodes)
            {
                List<String> list = new List<string>();
                String objectName = node.ParentNode.ParentNode.Attributes["Name"].Value;
                String screenName = node.ParentNode.ParentNode.ParentNode.ParentNode.Attributes["Name"].Value;
                if (!g_dictionary.ContainsKey(screenName))
                {
                    list.Add(objectName);
                    g_dictionary.Add(screenName, list);
                }
                else
                {
                    g_dictionary[screenName].Add(objectName);
                }
            }
            ModifyObjectNames(type);
            return g_objectList;
        }

        public void ModifyObjectNames(String type)
        {
            foreach (XmlNode node in config.SelectNodes("//Screen"))
            {
                String screenName = String.Empty;
                if (node.Name.Equals("Screen") && (node.Attributes["ScreenName"] != null || node.Attributes["Name"] != null))
                {
                    if (node.Attributes["DataNamePrefix"] != null)
                    {
                        String dataNamePrefixValue = node.Attributes["DataNamePrefix"].Value;

                        if (node.Attributes["ScreenName"] != null)
                        {
                            screenName = node.Attributes["ScreenName"].Value;
                        }
                        else
                        {
                            screenName = node.Attributes["Name"].Value;
                        }
                        if (g_dictionary.ContainsKey(screenName))
                        {
                            foreach (String objectName in g_dictionary[screenName])
                            {
                                if (g_objectList.ContainsKey(dataNamePrefixValue + objectName))
                                {
                                    continue;
                                }
                                else
                                {
                                    g_objectList.Add(dataNamePrefixValue + objectName, screenName);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (node.Attributes["ScreenName"] != null)
                        {
                            screenName = node.Attributes["ScreenName"].Value;
                        }
                        else
                        {
                            screenName = node.Attributes["Name"].Value;
                        }
                        if (g_dictionary.ContainsKey(screenName))
                        {
                            foreach (String objectName in g_dictionary[screenName])
                            {
                                if (g_objectList.ContainsKey(objectName))
                                {
                                    continue;
                                }
                                else
                                {
                                    g_objectList.Add(objectName, screenName);
                                }
                            }
                        }
                    }
                }
            }
            WriteToFile(g_objectList, type);
        }

        private void WriteToFile(Dictionary<String, String> dictionary, String type)
        {

            String path = objectLogFilePath;
            String fileName = String.Empty;
            FileStream fs;    
            if(!Path.HasExtension(path))
            {
                if (type.Equals("*"))
                {
                    fileName = "AllObjectsList.txt";
                }
                else
                {
                    fileName = String.Format("{0}ObjectList.txt", type);
                }
                fileName = Path.Combine(path, fileName);
            }
            else if(Path.HasExtension(path))
            {
                fileName = path;
            }
            if (!File.Exists(fileName))
            {
                fs = new FileStream(fileName, FileMode.Create, FileSystemRights.FullControl, FileShare.ReadWrite, 1024, FileOptions.None);
                fs.Close();
            }
            if (File.Exists(fileName))
            {
                StreamWriter writer = new StreamWriter(fileName);
                foreach (String objectName in dictionary.Keys)
                {
                    writer.WriteLine(objectName);
                }
                writer.Close();
                Process.Start("notepad.exe", fileName);
            }
            else
            {
                throw new Exception("File Not Found");
            }
            
        }
    }
}
