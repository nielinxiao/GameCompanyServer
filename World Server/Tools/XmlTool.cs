using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Word_Sever.Tools
{
    public static class XmlTool
    {
        public static bool AddNodes(string path,string title,params (string, string)[] KeyOrValues)
        {
            XmlDocument xmlDocument = new XmlDocument();
            XmlElement root = xmlDocument.CreateElement(title);
            xmlDocument.AppendChild(root);
            foreach (var stru in KeyOrValues)
            {
                XmlNode xmlNode = xmlDocument.CreateElement(stru.Item1);
                xmlNode.InnerText = stru.Item2;
                root.AppendChild(xmlNode);
            }
            try
            {
                xmlDocument.Save(path);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static (string,List<XmlNode>) ReadNodes(string path)
        {
            try
            {
                List<XmlNode> dict = new List<XmlNode>();
                XmlDocument xmlDocument=new XmlDocument();
                xmlDocument.Load(path);
                XmlElement root =xmlDocument.DocumentElement;
                for(int count=0;count<root.ChildNodes.Count;count++)
                {
                    XmlNode xmlNode = root.ChildNodes.Item(count);
                    dict.Add(xmlNode);
                }
                return (root.Name,dict);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return ("",null);
            }
        }
    }
}
