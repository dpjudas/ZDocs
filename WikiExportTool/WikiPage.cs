using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WikiExportTool
{
    internal class WikiPage
    {
        public string Id = "";
        public string Title = "";
        public bool Redirect = false;
        public string Content = "";

        public static SortedDictionary<string, WikiPage> LoadPages(string path)
        {
            var pages = new SortedDictionary<string, WikiPage>();
            foreach (string filename in Directory.GetFiles(path))
            {
                XDocument pageXml = XDocument.Parse(File.ReadAllText(filename, Encoding.UTF8));
                string ns = pageXml.Root.Name.NamespaceName;

                var page = new WikiPage();
                page.Id = pageXml.Root.Element(XName.Get("id", ns)).Value;
                page.Title = pageXml.Root.Element(XName.Get("title", ns)).Value;
                page.Redirect = pageXml.Root.Element(XName.Get("redirect", ns)) != null;
                var revision = pageXml.Root.Element(XName.Get("revision", ns));
                if (revision != null)
                {
                    var text = revision.Element(XName.Get("text", ns));
                    if (text != null)
                        page.Content = text.Value;
                }
                pages[page.Id] = page;
            }

            return pages;
        }
    }
}
