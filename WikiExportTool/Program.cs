using System;

namespace WikiExportTool
{
    class Program
    {
        static void PrintHelp()
        {
            Console.WriteLine("WikiExportTool export                      - Export all wiki pages as XML");
            Console.WriteLine("WikiExportTool convert <xml pages path>    - Convert all page XML files to HTML");
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
                    WikiExporter.ExportPages();
                    WikiExporter.ExportCategories();
                    return 0;
                }
                else if (command == "convert" && args.Length == 2)
                {
                    HtmlPageConverter.ConvertPages(args[1]);
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
