using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace WikiExportTool
{
    internal enum WikiTextTokenType
    {
        Text,
        NewLine, // \n
        Comment, // <!-- -->
        XmlTagOpen, // <tag>
        XmlTagClose, // </tag>
        Italic, // ''italics''
        Bold, // '''bold'''
        BoldItalic, // '''''
        Heading1, // = == === ==== ===== ======
        Heading2,
        Heading3,
        Heading4,
        Heading5,
        Heading6,
        Link, // [[ ]]
        Command, // {{ }}
    }

    internal class WikiToken
    {
        public WikiTextTokenType Type;
        public string Value;
    }

    internal class WikiText
    {
        static bool CheckToken(string tokenBegin, string tokenEnd, WikiTextTokenType type, string text, ref int i, ref int lastpos, List<WikiToken> tokens)
        {
            if (text[i] != tokenBegin[0] || !text.Substring(i).StartsWith(tokenBegin))
                return false;

            int end = text.Substring(i + tokenBegin.Length).IndexOf(tokenEnd);
            if (end == -1)
                return false;

            if (i != lastpos)
                tokens.Add(new WikiToken { Type = WikiTextTokenType.Text, Value = text.Substring(lastpos, i - lastpos) });

            tokens.Add(new WikiToken { Type = type, Value = text.Substring(i + tokenBegin.Length, end) });

            i += tokenBegin.Length + end + tokenEnd.Length;
            lastpos = i;
            return true;
        }

        public static List<WikiToken> Parse(string text)
        {
            var tokens = new List<WikiToken>();
            int i = 0;
            int lastpos = 0;
            while (i < text.Length)
            {
                if (CheckToken("<!--", "-->", WikiTextTokenType.Comment, text, ref i, ref lastpos, tokens)) continue;
                if (CheckToken("'''''", "'''''", WikiTextTokenType.BoldItalic, text, ref i, ref lastpos, tokens)) continue;
                if (CheckToken("'''", "'''", WikiTextTokenType.Bold, text, ref i, ref lastpos, tokens)) continue;
                if (CheckToken("''", "''", WikiTextTokenType.Italic, text, ref i, ref lastpos, tokens)) continue;
                if (CheckToken("[[", "]]", WikiTextTokenType.Link, text, ref i, ref lastpos, tokens)) continue;
                if (CheckToken("{{", "}}", WikiTextTokenType.Command, text, ref i, ref lastpos, tokens)) continue;

                if (i == 0 || text[i - 1] == '\n')
                {
                    if (CheckToken("======", "======", WikiTextTokenType.Heading6, text, ref i, ref lastpos, tokens)) continue;
                    if (CheckToken("=====", "=====", WikiTextTokenType.Heading5, text, ref i, ref lastpos, tokens)) continue;
                    if (CheckToken("====", "====", WikiTextTokenType.Heading4, text, ref i, ref lastpos, tokens)) continue;
                    if (CheckToken("===", "===", WikiTextTokenType.Heading3, text, ref i, ref lastpos, tokens)) continue;
                    if (CheckToken("==", "==", WikiTextTokenType.Heading2, text, ref i, ref lastpos, tokens)) continue;
                    if (CheckToken("=", "=", WikiTextTokenType.Heading1, text, ref i, ref lastpos, tokens)) continue;
                }

                if (text[i] == '*')
                {
                    // To do: list item
                }

                if (text[i] == '<')
                {
                    // To do: open and close tags
                }

                if (text[i] == '\n')
                {
                    if (i != lastpos)
                        tokens.Add(new WikiToken { Type = WikiTextTokenType.Text, Value = text.Substring(lastpos, i - lastpos) });
                    tokens.Add(new WikiToken { Type = WikiTextTokenType.NewLine, Value = "\n" });
                    i++;
                    lastpos = i;
                    continue;
                }

                i++;
            }

            if (i != lastpos)
                tokens.Add(new WikiToken { Type = WikiTextTokenType.Text, Value = text.Substring(lastpos, i - lastpos) });

            return tokens;
        }
    }
}
