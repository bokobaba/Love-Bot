using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Love_Bot.Sites {
    static class WebsiteUtils {
        public static Dictionary<string, string> states = new Dictionary<string, string>() {
            {"AL", "Alabama" },
            {"AK", "Alaska" },
            {"AZ", "Arizona" },
            {"AR", "Arkansas" },
            {"CA", "California" },
            {"CO", "Colorado" },
            {"CT", "Connecticut" },
            {"DE", "Delaware" },
            {"FL", "Florida" },
            {"GA", "Georgia" },
            {"HI", "Hawaii" },
            {"ID", "Idaho" },
            {"IL", "Illinois" },
            {"IN", "Indiana" },
            {"IA", "Iowa" },
            {"KS", "Kansas" },
            {"KY", "Kentucky" },
            {"LA", "Louisiana" },
            {"ME", "Maine" },
            {"MD", "Maryland" },
            {"MA", "Massachusetts" },
            {"MI", "Michigan" },
            {"MN", "Minnesota" },
            {"MS", "Mississippi" },
            {"MO", "Missouri" },
            {"MT", "Montana" },
            {"NE", "Nebraska" },
            {"NV", "Nevada" },
            {"NH", "New Hampshire" },
            {"NJ", "New Jersey" },
            {"NM", "New Mexico" },
            {"NY", "New York" },
            {"NC", "North Carolina" },
            {"ND", "North Dakota" },
            {"OH", "Ohio" },
            {"OK", "Oklahoma" },
            {"OR", "Oregon" },
            {"PA", "Pennsylvania" },
            {"RI", "Rhode Island" },
            {"SC", "South Carolina" },
            {"SD", "South Dakota" },
            {"TN", "Tennessee" },
            {"TX", "Texas" },
            {"UT", "Utah" },
            {"VT", "Vermont" },
            {"VA", "Virginia" },
            {"WA", "Washington" },
            {"WV", "West Virginia" },
            {"WI", "Wisconsin" },
            {"WY", "Wyomin" }
        };
        public static void WriteNode(StreamWriter file, HtmlNode node, int indentLevel) {
            // check parameter
            if (file == null) return;
            if (node == null) return;

            // init 
            string INDENT = " ";
            string NEW_LINE = Environment.NewLine;

            // case: no children
            if (node.HasChildNodes == false) {
                for (int i = 0; i < indentLevel; i++)
                    file.Write(INDENT);
                file.Write(node.OuterHtml);
                file.Write(NEW_LINE);
            }

            // case: node has childs
            else {
                // indent
                for (int i = 0; i < indentLevel; i++)
                    file.Write(INDENT);

                // open tag
                file.Write(string.Format("<{0} ", node.Name));
                if (node.HasAttributes)
                    foreach (var attr in node.Attributes)
                        file.Write(string.Format("{0}=\"{1}\" ", attr.Name, attr.Value));
                file.Write(string.Format(">{0}", NEW_LINE));

                // childs
                foreach (var chldNode in node.ChildNodes)
                    WriteNode(file, chldNode, indentLevel + 1);

                // close tag
                for (int i = 0; i < indentLevel; i++)
                    file.Write(INDENT);
                file.Write(string.Format("</{0}>{1}", node.Name, NEW_LINE));
            }
        }

        public static string GetHtmlContent(string url) {

            int attempts = 0;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "GET";
            request.Timeout = 20000;
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.198 Safari/537.36";
            request.Accept = "text/html";
            request.ContentType = "application/json";
            request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-us,en;q=0.5");
            request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
            request.Headers.Add("Upgrade-Insecure-Requests", "1");
            request.CookieContainer = new CookieContainer();

            HttpWebResponse response;
            try {
                response = request.GetResponse() as HttpWebResponse;
            } catch (WebException ex) {
                response = ex.Response as HttpWebResponse;
            }

            Console.WriteLine("web response = " + (response).StatusDescription);

            Stream responseStream = response.GetResponseStream();

            if (response.ContentEncoding?.IndexOf("gzip", StringComparison.InvariantCultureIgnoreCase) >= 0) {
                responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
            } else if (response.ContentEncoding?.IndexOf("deflate", StringComparison.InvariantCultureIgnoreCase) >= 0) {
                responseStream = new DeflateStream(responseStream, CompressionMode.Decompress);
            }


            using (MemoryStream ms = new MemoryStream()) {
                responseStream?.CopyTo(ms);

                string htmlContent = Encoding.UTF8.GetString(ms.ToArray());
                //Console.WriteLine(System.Xml.Linq.XElement.Parse(htmlContent).ToString());

                response.Close();

                return htmlContent;
            }
        }
    }
}
