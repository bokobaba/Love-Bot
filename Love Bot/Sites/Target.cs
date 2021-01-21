using HtmlAgilityPack;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Love_Bot.Sites {
    class Target : Website {
        private static readonly string
            cartUrl = "https://www.target.com/co-cart",
            loginUrl = "https://www.target.com/account",
            logoutUrl = "https://www.target.com",
            itemNameXpath = "//h1[@data-test='product-title']",
            itemPriceXpath = "//div[@data-test='product-price']",
            itemButtonXpath = "//button[@data-test='shipItButton']";

        public Target(string name, WebsiteConfig config, Dictionary<string, Dictionary<string, string>> cred)
            : base(name, config, cred) {
        }

        protected override string AddToCartText {
            get {
                return "ship it";
            }
        }

        protected override bool AddToCart(string url, bool refresh = false) {
            Console.WriteLine(name + ": adding product to Target cart");
            Console.ReadLine();
            return true;
        }

        protected override bool Checkout() {
            Console.WriteLine(name + ": checkout Target");
            throw new NotImplementedException();
        }

        protected override bool Login(string email, string password) {
            Console.WriteLine(name + ": login to Target");

            Console.ReadLine();
            Console.WriteLine(name + ": searching for account header");
            IWebElement elem = FindElementTimeout(5, x => driver.FindElementByXPath(x), "//span[@data-test='accountUserName']");
            if (elem is null) return false;

            driver.Navigate().GoToUrl(loginUrl);

            elem.Click();

            Console.WriteLine(name + ": searching for signin button");
            elem = FindElementTimeout(5, x => elem.FindElement(By.XPath(x)), "//div[contains(text(), 'Sign in')]");
            if (elem is null) return false;
            elem.Click();


            Console.WriteLine(name + ": entering email");

            elem = FindElementTimeout(5, x => driver.FindElementById(x), "username");
            if (elem is null) return false;

            TryInvokeElement(5, () => { elem.SendKeys(Keys.Control + "a"); });
            elem.SendKeys(email);

            Console.WriteLine(name + ": entering password");

            elem = FindElementTimeout(5, x => driver.FindElementById(x), "password");
            if (elem is null) return false;

            elem.SendKeys(Keys.Control + "a");
            elem.SendKeys(password);

            Task.Delay(500).Wait();

            elem.SendKeys(Keys.Enter);

            WaitUntilStale(5, elem, () => { bool b = elem.Displayed || elem.Enabled; });


            Console.WriteLine(name + ": Login Successful");
            Console.ReadLine();
            return true;
        }

        protected override Product ParseBrowser(string url) {
            throw new NotImplementedException();
        }

        protected override Product ParseNoBrowser(string url) {
            Console.WriteLine(name + ": checking target");
            string html = WebsiteUtils.GetHtmlContent(url);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            //StreamWriter file = new StreamWriter(@"C:\Test\test.txt");
            //if (doc.DocumentNode != null)
            //    foreach (var n in doc.DocumentNode.ChildNodes)
            //        WebsiteUtils.WriteNode(file, n, 0);
            //file.Close();

            Product product = new Product();

            HtmlNode node = doc.DocumentNode.SelectSingleNode(itemNameXpath);

            if (node != null) {
                //Console.WriteLine("\n name:\n" + node.InnerText);
                product.name = node.InnerText == null ? "null" : node.InnerText;
            }
            else {
                //Console.WriteLine("\n name:\not found");
                product.name = "not found";
            }

            node = doc.DocumentNode.SelectSingleNode(itemPriceXpath);
            if (node != null) {
                //Console.WriteLine("\n price:\n" + node.InnerText);
                NumberStyles style = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;
                CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
                float number;
                product.price = float.TryParse(node.InnerText, style, culture, out number) ? number : float.MaxValue;
            }
            else {
                //Console.WriteLine("\nprice:\nnot found");
                product.price = float.MaxValue;
            }

            node = doc.DocumentNode.SelectSingleNode(itemButtonXpath);
            if (node != null) {
                Console.WriteLine(node.InnerText);
                //Console.WriteLine("\n button:\n" + node.Attributes["value"].Value);
                //product.button = node.Attributes["value"] == null ? "null" : node.Attributes["value"].Value;
                product.button = node.InnerText == null ? "null" : node.InnerText;
            }
            else {
                //Console.WriteLine("\n button: not found\n");
                product.button = "not found";
            }

            product.link = url;

            return product;
        }
    }
}
