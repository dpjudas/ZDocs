using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PodcastDownloader
{
    struct AllPagesItem
    {
        public string pageId;
        public string title;
    }

    class Program
    {
        const string apiUrl = "https://zdoom.org/w/api.php?";

        static List<AllPagesItem> GetAllPages()
        {
            Console.Write("Retrieving page list from the wiki");

            using (var client = new HttpClient())
            {
                string continueKey = "apcontinue";
                string continueValue = "";

                var pages = new List<AllPagesItem>();

                while (true)
                {
                    NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
                    queryString.Add("action", "query");
                    queryString.Add("list", "allpages");
                    queryString.Add("format", "xml");
                    queryString.Add("aplimit", "500");
                    if (continueValue != "")
                        queryString.Add(continueKey, continueValue);
                    string url = apiUrl + queryString.ToString();

                    XDocument doc = XDocument.Parse(client.GetStringAsync(url).Result);
                    foreach (XElement item in doc.Element("api").Element("query").Element("allpages").Elements("p"))
                    {
                        var page = new AllPagesItem();
                        page.pageId = item.Attribute("pageid").Value;
                        page.title = item.Attribute("title").Value;
                        pages.Add(page);
                    }

                    XElement continueElement = doc.Element("api").Element("continue");
                    if (continueElement == null || string.IsNullOrEmpty(continueElement.Attribute("continue").Value))
                        break;
                    continueValue = continueElement.Attribute(continueKey).Value;

                    Console.Write(".");

                    // Don't rape the wiki while we do this
                    Thread.Sleep(1000);
                }

                Console.WriteLine("");

                return pages;
            }
        }

        static string ExportPages(IEnumerable<string> pageIds)
        {
            var builder = new StringBuilder();
            foreach (string s in pageIds)
            {
                if (builder.Length > 0)
                    builder.Append("|");
                builder.Append(s);
            }
            string ids = builder.ToString();

            using (var client = new HttpClient())
            {
                NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
                queryString.Add("action", "query");
                queryString.Add("export", "1");
                queryString.Add("exportnowrap", "1");
                queryString.Add("format", "xml");
                queryString.Add("pageids", ids);
                string url = apiUrl + queryString.ToString();
                return client.GetStringAsync(url).Result;
            }
        }

        public static IEnumerable<List<T>> SplitList<T>(List<T> locations, int nSize)
        {
            for (int i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
            }
        }

        public sealed class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }

        static string FilterFilename(string filename)
        {
            // Keep this US english friendly as it is just a hint anyway

            var builder = new StringBuilder(filename.Length);
            foreach (char c in filename)
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                    builder.Append(c);
                else if (c == ' ')
                    builder.Append('-');
                else
                    builder.Append('_');
            }
            return builder.ToString();
        }

        static void ExportPages()
        {
            Console.WriteLine("");

            List<AllPagesItem> allPages = GetAllPages();

            Console.Write("Exporting pages");

            foreach (List<AllPagesItem> pagesChunk in SplitList(allPages, 30))
            {
                XDocument exportXml = XDocument.Parse(ExportPages(pagesChunk.Select(p => p.pageId)));
                string exportNamespace = exportXml.Root.Name.NamespaceName;

                foreach (XElement item in exportXml.Element(XName.Get("mediawiki", exportNamespace)).Elements(XName.Get("page", exportNamespace)))
                {
                    string id = item.Element(XName.Get("id", exportNamespace)).Value;
                    string title = item.Element(XName.Get("title", exportNamespace)).Value;

                    string filename = id + "_" + FilterFilename(title) + ".xml";

                    using (StringWriter sw = new Utf8StringWriter())
                    {
                        item.Save(sw, SaveOptions.None);
                        File.WriteAllText(filename, item.ToString(), new UTF8Encoding(false));
                    }
                }

                Console.Write(".");

                // Don't rape the wiki while we do this
                Thread.Sleep(1000);
            }

            Console.WriteLine("");
        }

        static void ConvertPages()
        {
            Console.WriteLine("Not implemented yet");
        }

        static void PrintHelp()
        {
            Console.WriteLine("WikiExportTool export        - Export all wiki pages as XML");
            Console.WriteLine("WikiExportTool convert       - Convert all page XML files to Markdown");
        }

        static int Main(string[] args)
        {
            try
            {
                string command = "help";
                if (args.Length > 0)
                    command = args[0];

                if (command == "export")
                {
                    ExportPages();
                    return 0;
                }
                else if (command == "convert")
                {
                    ConvertPages();
                    return 0;
                }
                else
                {
                    PrintHelp();
                    return 1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return 1;
            }
        }
    }
}
