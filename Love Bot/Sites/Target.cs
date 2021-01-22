using HtmlAgilityPack;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Love_Bot.Sites {
    class Target : Website {
        private static readonly string
            cartUrl = "https://www.target.com/co-cart",
            loginUrl = "https://www.target.com",
            itemNameXpath = "//h1[@data-test='product-title']",
            itemPriceXpath = "//div[@data-test='product-price']",
            itemButtonXpath = "//button[@data-test='shipItButton']";

        public Target(string name, WebsiteConfig config, Dictionary<string, Dictionary<string, string>> cred)
            : base(name, config, cred) {
            config.loadBrowserOnStart = true;
        }

        protected override string AddToCartText {
            get {
                return "ship it";
            }
        }

        public override void UpdateConfigs(WebsiteConfig newConfig) {
            base.UpdateConfigs(newConfig);
            config.loadBrowserOnStart = true;
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
            driver.Navigate().GoToUrl(loginUrl);

            Console.WriteLine(name + ": searching for account header");
            IWebElement elem = FindElementTimeout(5, x => driver.FindElementByXPath(x), "//span[@data-test='accountUserName']");
            if (elem is null) return false;
            elem.Click();
            if (!elem.GetAttribute("innerText").Equals("Sign in")) {
                Console.WriteLine(name + ": signing out");
                elem = FindElementTimeout(5, x => driver.FindElement(By.Id(x)), "accountNav-guestSignOut");
                if (elem is null) return false;
                Task.Delay(2000).Wait();
                elem.Click();
                WaitUntilStale(5, elem, () => { bool b = elem.Displayed || elem.Enabled; });
                Login(email, password);
            }

            Console.WriteLine(name + ": searching for signin button");
            elem = FindElementTimeout(5, x => driver.FindElement(By.Id(x)), "accountNav-signIn");
            if (elem is null) return false;
            Task.Delay(2000).Wait();
            elem.Click();


            Console.WriteLine(name + ": entering email");

            elem = FindElementTimeout(5, x => driver.FindElementById(x), "username");
            if (elem != null) {
                TryInvokeElement(5, () => { elem.SendKeys(Keys.Control + "a"); });
                elem.SendKeys(email);
            } else {
                Console.WriteLine(name + ": no username field found");
            }

            Console.WriteLine(name + ": entering password");

            elem = FindElementTimeout(5, x => driver.FindElementById(x), "password");
            if (elem is null) return false;

            elem.SendKeys(Keys.Control + "a");
            elem.SendKeys(password);

            Task.Delay(500).Wait();

            elem.SendKeys(Keys.Enter);

            WaitUntilStale(5, elem, () => { bool b = elem.Displayed || elem.Enabled; });


            Console.WriteLine(name + ": Login Successful");
            return true;
        }

        protected override Product ParseBrowser(string url) {
            Console.WriteLine(name + ": checking Target");
            AddToCartButton = null;
            driver.Navigate().GoToUrl(url);
            Product product = new Product();
            product.link = url;

            Task.Delay(2000).Wait();
            IWebElement elem = FindElementTimeout(3, x => driver.FindElementByXPath(x), itemNameXpath);
            if (elem != null) {
                TryInvokeElement(5, () => { product.name = elem.GetAttribute("innerText").Trim(); });
            }

            elem = FindElementTimeout(1, x => driver.FindElementByXPath(x), itemPriceXpath);
            if (elem != null) {
                //Console.WriteLine("price = [" + elem.GetAttribute("innerText") +  "]");
                float number;
                product.price = float.TryParse(elem.GetAttribute("innerText"), style, culture, out number) ? number : Single.NaN;
            }

            elem = FindElementTimeout(1, x => driver.FindElementByXPath(x), itemButtonXpath);
            if (elem != null) {
                product.button = elem.Text.ToLower();
                AddToCartButton = elem;
            }

            return product;
        }

        protected override Product ParseNoBrowser(string url) {
            throw new NotImplementedException();
        }
    }
}
