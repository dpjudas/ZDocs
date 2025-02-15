using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikiExportTool
{
    internal class HtmlPageConverter
    {
		class ContentItem
		{
			public ContentItem(string name, string src) { this.name = name; this.src = src; }
            public string name;
			public string src;
			public List<ContentItem> items = new List<ContentItem>();
		}

		class SearchIndex
		{
			public List<ContentItem> items = new List<ContentItem>();
			public Dictionary<string, List<int>> keys = new Dictionary<string, List<int>>();
		}

        public static void ConvertPages(string pagesPath)
        {
            var contentList = new List<ContentItem>();
            var searchIndex = new SearchIndex();
           
			SortedDictionary<string, WikiPage> pages = WikiPage.LoadPages(pagesPath);
            foreach (string id in pages.Keys)
			{
				WikiPage page = pages[id];

				if (page.Redirect)
					continue;

				string pageFilename = page.Id + ".html";
                File.WriteAllText(pageFilename, CreatePageHtml(page), new UTF8Encoding(false));

				int index = searchIndex.items.Count;
                searchIndex.items.Add(new ContentItem(page.Title, pageFilename));
				if (searchIndex.keys.ContainsKey(page.Title))
					searchIndex.keys[page.Title].Add(index);
				else
					searchIndex.keys[page.Title] = new List<int> { index };
            }

            var categories = JArray.Parse(File.ReadAllText(Path.Combine(pagesPath, "categories.json"), new UTF8Encoding(false)));
            foreach (JObject item in categories)
			{
				var categoryName = (string)item["name"];
				var categoryItem = new ContentItem(categoryName, "");
				foreach (string pageId in item["pages"])
				{
					if (pages.ContainsKey(pageId))
					{
						WikiPage page = pages[pageId];
						if (!page.Redirect)
						{
							string pageFilename = page.Id + ".html";
							categoryItem.items.Add(new ContentItem(page.Title, pageFilename));
						}
					}
                }
                contentList.Add(categoryItem);
            }

            File.WriteAllText("index.html", IndexHtml, new UTF8Encoding(false));
            File.WriteAllText("index.css", IndexCss, new UTF8Encoding(false));
            File.WriteAllText("index.js", CreateJS(contentList, searchIndex), new UTF8Encoding(false));
        }

		static string CreatePageHtml(WikiPage page)
		{
			int listItemDepth = 0;
			int newlineCount = 0;

            var content = new StringBuilder();
			content.Append("<h1>");
			content.Append(page.Title);
			content.Append("</h1>");
            content.Append("<content-view>");
			foreach (WikiToken token in WikiText.Parse(page.Content))
			{
				if (newlineCount > 1 && token.Type != WikiTextTokenType.ListItem)
				{
                    while (listItemDepth > 0)
                    {
                        content.Append("</ul>");
                        listItemDepth--;
                    }
                }

                if (token.Type != WikiTextTokenType.NewLine)
					newlineCount = 0;
				else
					newlineCount++;

				if (token.Type == WikiTextTokenType.Heading1)
				{
					content.Append("<h1>");
					content.Append(token.Value);
					content.Append("</h1>");
				}
				else if (token.Type == WikiTextTokenType.Heading2)
				{
					content.Append("<h2>");
					content.Append(token.Value);
					content.Append("</h2>");
				}
				else if (token.Type == WikiTextTokenType.Heading3)
				{
					content.Append("<h3>");
					content.Append(token.Value);
					content.Append("</h3>");
				}
				else if (token.Type == WikiTextTokenType.Heading4)
				{
					content.Append("<h4>");
					content.Append(token.Value);
					content.Append("</h4>");
				}
				else if (token.Type == WikiTextTokenType.Heading5)
				{
					content.Append("<h5>");
					content.Append(token.Value);
					content.Append("</h5>");
				}
				else if (token.Type == WikiTextTokenType.Heading6)
				{
					content.Append("<h6>");
					content.Append(token.Value);
					content.Append("</h6>");
				}
				else if (token.Type == WikiTextTokenType.Italic)
				{
					content.Append("<i>");
					content.Append(token.Value);
					content.Append("</i>");
				}
				else if (token.Type == WikiTextTokenType.Bold)
				{
					content.Append("<b>");
					content.Append(token.Value);
					content.Append("</b>");
				}
				else if (token.Type == WikiTextTokenType.BoldItalic)
				{
					content.Append("<b><i>");
					content.Append(token.Value);
					content.Append("</i></b>");
				}
				else if (token.Type == WikiTextTokenType.Comment)
				{
				}
                else if (token.Type == WikiTextTokenType.Link)
                {
					int splitter = token.Value.IndexOf('|');
					string pageName;
					string linkText;
					if (splitter == -1)
					{
						pageName = token.Value;
						linkText = token.Value;
					}
					else
					{
						pageName = token.Value.Substring(0, splitter);
						linkText = token.Value.Substring(splitter + 1);
					}
					content.Append("<a href=\"");
					content.Append(pageName);
					content.Append("\">");
                    content.Append(linkText);
                    content.Append("</a>");
                }
                else if (token.Type == WikiTextTokenType.Command)
                {
                    content.Append("<b>{{");
                    content.Append(token.Value);
                    content.Append("}}</b>");
                }
                else if (token.Type == WikiTextTokenType.XmlTagOpen)
                {
                }
                else if (token.Type == WikiTextTokenType.XmlTagClose)
                {
                }
                else if (token.Type == WikiTextTokenType.ListItem)
                {
					while (listItemDepth < token.Depth)
					{
                        content.Append("<ul>");
                        listItemDepth++;
                    }
                    while (listItemDepth > token.Depth)
                    {
                        content.Append("</ul>");
                        listItemDepth--;
                    }
                    content.Append("<li>");
                }
                else if (token.Type == WikiTextTokenType.NewLine)
                {
                    content.Append("<p>");
                }
                else
                {
					content.Append(token.Value);
				}
            }
            content.Append("</content-view>");

            return PageHtml
				.Replace("$title", page.Title)
				.Replace("$content", content.ToString());
        }

		static JObject CreateContentItem(ContentItem item)
		{
            var itemJson = new JObject();
            itemJson["name"] = item.name;
            itemJson["src"] = item.src;
			if (item.items.Count > 0)
			{
                var itemsJson = new JArray();
				foreach (ContentItem child in item.items)
					itemsJson.Add(CreateContentItem(child));
				itemJson["items"] = itemsJson;
            }
            return itemJson;
        }

        static string CreateJS(List<ContentItem> contentList, SearchIndex searchIndex)
		{
            var contentsJson = new JArray();
			foreach (ContentItem item in contentList)
			{
                contentsJson.Add(CreateContentItem(item));
            }

            var searchIndexItems = new JArray();
            foreach (ContentItem item in searchIndex.items)
            {
                var itemJson = new JObject();
                itemJson["name"] = item.name;
                itemJson["src"] = item.src;
                searchIndexItems.Add(itemJson);
            }

            var searchIndexKeys = new JObject();
            foreach (string key in searchIndex.keys.Keys)
            {
                var value = new JArray();
				foreach (int index in searchIndex.keys[key])
				{
					value.Add(index);
				}
				searchIndexKeys[key.ToLowerInvariant()] = value;
            }

            var searchIndexJson = new JObject();
			searchIndexJson["items"] = searchIndexItems;
            searchIndexJson["keys"] = searchIndexKeys;

            var js = new StringBuilder();

            js.Append("var contents = ");
			js.Append(contentsJson.ToString(Newtonsoft.Json.Formatting.Indented));
            js.AppendLine(";");

            js.Append("var searchIndex = ");
            js.Append(searchIndexJson.ToString(Newtonsoft.Json.Formatting.Indented));
            js.AppendLine(";");

            js.Append(IndexJS);

			return js.ToString();
        }

        const string PageHtml = @"<!DOCTYPE html>
<html>
<head>
	<meta charset=""utf-8"" />
	<title>$title</title>
	<link href=""index.css"" rel=""stylesheet"" type=""text/css"" />
</head>
<body class=""page"">
$content
</body>
</html>";

        const string IndexHtml = @"<!DOCTYPE html>
<html>
<head>
	<meta charset=""utf-8"" />
	<title>ZDoom Manual</title>
	<link href=""index.css"" rel=""stylesheet"" type=""text/css"" />
</head>
<body class=""main hbox"">
	<div id=""sidepanel"" class=""sidepanel view vbox"">
		<input id=""searchbox"" class=""searchbox view"" type=""text"" placeholder=""Search for..."" />
		<div id=""listview"" class=""listview view expanding""></div>
	</div>
	<iframe id=""content"" class=""content expanding""></iframe>
	<script src=""index.js""></script>
</body>
</html>";

        const string IndexCss = @"
/***************************************************************************/
/* reset css styling */

body, header, footer, section, h1, h2, h3, h4, h5, p, ul, ol, li, menu, menuitem, iframe { margin: 0; border: none; padding: 0; }
h1, h2, h3, h4, h5 { font-family: inherit; font-weight: inherit; font-weight: inherit; text-decoration: inherit; font-style: inherit; font-size: inherit; line-height: inherit; }

/***************************************************************************/
/* base page style */

body { background: #333; font: 12px/16px ""Segoe UI"", ""Tahoma"", sans-serif; color: #eee; }
a, a:hover, a:visited { color: orange; text-decoration: none; }
a:hover { color: orange; }

/***************************************************************************/
/* controls */

.vbox { display: flex; flex-direction: column; overflow: hidden; }
.hbox { display: flex; flex-direction: row; }
.view { position: relative; flex: 0 0 auto; }
.expanding { flex: 1 1 0; }
.modallayer { position: fixed; left: 0; top: 0; width: 100vw; height: 100vh; }

.main { width: 100vw; height: 100vh; }

.sidepanel {
	width: 300px;
	background: #555;
	border-right: 1px solid #333;
}

.searchbox {
	margin: 10px 10px 5px 10px;
	padding: 2px 10px;
	border: 1px solid #666;
	background: #333;
	line-height: 25px;
	color: #eee;
}

.searchbox:focus {
	outline: none;
	border-color: rgb(150,150,255);
}

.listview {
	overflow-y: scroll;
	padding: 5px 0;
}

.listviewitem {
}

.listviewitem-text {
	padding: 5px 15px;
	cursor: pointer;
}

.listviewitem.selected > .listviewitem-text {
	font-weight: bold;
}

.listviewitem-text:hover {
	background: rgb(60,60,130);
}

.content {
	border-left: 1px solid #333;
}

.page {
	margin: 15px;
	font: 14px/20px ""Segoe UI"", ""Tahoma"", sans-serif;

	h1 { margin: 15px 0; font-size: 1.3em; font-weight: bold; }
	h2 { margin: 20px 0 15px 0; font-size: 1.2em; font-weight:bold; font-style: italic }
	h3 { margin: 15px 0 10px 0; font-weight: bold; }
	ul { padding-left: 20px; }
	p { margin: 12px 0; }
	pre { white-space: pre-line; }

	content-view {
		line-height: 18px;
	}
}

";

        const string IndexJS = @"

var searchbox = document.getElementById(""searchbox"");
var listview = document.getElementById(""listview"");
var content = document.getElementById(""content"");
var listviewItems = [];

function search(searchString) {
	var words = searchString.split(/\W+/);
	if (words.length > 0 && words[words.length-1] == """") words.splice(-1);
	if (words.length == 0) return [];

	var results = {};
	var totalWords = 1;
	var lastword = words.splice(-1)[0].toLowerCase();

	// Search whole word results for everything except last word
	words.forEach(word => {
		if (word != """") {
			var key = word.toLowerCase();
			if (searchIndex.keys[key] != undefined) {
				searchIndex.keys[key].forEach(x => {
					if (results[x] == undefined) results[x] = 1;
					else results[x]++;
				});
			}
			totalWords++;
		}
	});

	// Partial word search for the last word
	Object.keys(searchIndex.keys).forEach(key => {
		if (key.length >= lastword.length && key.substr(0, lastword.length) == lastword) {
			searchIndex.keys[key].forEach(x => {
				if (totalWords == 1 || results[x] == totalWords - 1) results[x] = totalWords;
			});
		}
	});

	// Only match pages that included all the words
	var output = [];
	Object.keys(results).forEach(x => { if (results[x] == totalWords) output.push(searchIndex.items[x]) });
	output.sort((a, b) => a.name.localeCompare(b.name));
	return output;
}

function generateIndex() {
	function addItems(list, output) {
		list.forEach(x => {
			output.push(x);
			if (x.items != undefined) addItems(x.items, output);
		});
		return output;
	}
	var items = addItems(contents, []);
	var curIndex = 0;
	
	searchIndex = {
		items: items.map(x => { return { name: x.name, src: x.src } }),
		keys: {}
	};
	
	var timeoutID;
	
	function done() {
		content.removeEventListener('load', contentLoaded);
		content.removeAttribute(""src"");
		
		content.setAttribute(""srcdoc"", JSON.stringify(searchIndex));
	}
	
	function next() {
		curIndex++;
		if (curIndex < items.length) {
			timeoutID = setTimeout(contentFailedLoad, 2000);
			content.setAttribute(""src"", items[curIndex].src);
		}
		else {
			done();
		}
	}
	
	function contentFailedLoad() {
		// console.debug(""Failed to load "" + items[curIndex].name + "" ("" + items[curIndex].src + "")"");
		next();
	}

	function contentLoaded(event) {
		clearTimeout(timeoutID);

		if (content.contentDocument == null) {
			content.removeEventListener('load', contentLoaded);
			alert(""security.fileuri.strict_origin_policy needs to be enabled for generateIndex() to work (remember to turn it off again after!)"");
			return;
		}
		
		var wordSet = {};
		content.contentDocument.body.innerText.split(/\W+/).forEach(word => {
			if (word != """") wordSet[word.toLowerCase()] = true;
		});
		
		Object.keys(wordSet).forEach(word => {
			if (searchIndex.keys[word] == undefined) searchIndex.keys[word] = [];
			searchIndex.keys[word].push(curIndex);
		});

		next();
	}
	timeoutID = setTimeout(contentFailedLoad, 2000);
	content.addEventListener('load', contentLoaded);
	content.setAttribute(""src"", items[curIndex].src);
}

function addClass(element, name) {
	var classes = getClasses(element);
	var index = classes.indexOf(name);
	if (index == -1) {
		classes.push(name);
		setClasses(element, classes);
	}
}

function removeClass(element, name) {
	var classes = getClasses(element);
	var index = classes.indexOf(name);
	if (index != -1) {
		classes.splice(index, 1);
		setClasses(element, classes);
	}
}

function getClasses(element) {
	var classes = element.getAttribute(""class"");
	if (classes == null) return [];
	return classes.split("" "");
}

function setClasses(element, classes) {
	element.setAttribute(""class"", classes.join("" ""));
}

function filterItems(filter) {
	while (listview.lastChild != null) {
		listview.removeChild(listview.lastChild);
	}
	listviewItems = [];
	
	if (filter == """") {
		contents.forEach(item => addItem(listview, item, 1));
	}
	else {
		search(filter).forEach(item => addItem(listview, item, 1));
	}
}

function itemSelected(element, item) {
	listviewItems.forEach(x => removeClass(x, ""selected""));
	addClass(element, ""selected"");
	content.setAttribute(""src"", item.src);

	if (item.items != undefined) {
		if (item.open) {
			item.open = false;
			item.childrenDiv.style.display = ""none"";
		}
		else {
			item.open = true;
			item.childrenDiv.style.display = """";
		}
	}
}

function addItem(parent, item, level) {
	var element = document.createElement(""div"");
	var text = document.createElement(""div"");
	addClass(element, ""listviewitem"");
	addClass(text, ""listviewitem-text"");
	text.style.paddingLeft = (level * 20) + ""px"";
	text.innerText = item.name;
	text.addEventListener('click', event => itemSelected(element, item, event) );
	element.appendChild(text);
	listviewItems.push(element);
	parent.appendChild(element);
	if (item.items != undefined) {
		item.childrenDiv = document.createElement(""div"");
		item.childrenDiv.style.display = ""none"";
		item.open = false;
		parent.appendChild(item.childrenDiv);
		item.items.forEach(childitem => addItem(item.childrenDiv, childitem, level + 1));
	}
}

searchbox.addEventListener('input', event => filterItems(searchbox.value, event));
contents.forEach(item => addItem(listview, item, 1));

";
    }
}
