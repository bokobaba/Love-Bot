using HtmlAgilityPack;
using System;
using OpenQA.Selenium.Chrome;
using System.Threading.Tasks;
using System.Globalization;
using OpenQA.Selenium;
using System.Collections.Generic;
using OpenQA.Selenium.Support.UI;
using System.IO;

namespace Love_Bot.Sites {
    class Walmart : Website {
        private static readonly string
            checkoutUrl = "https://www.walmart.com/checkout/#/fulfillment",
            loginUrl = "https://www.walmart.com/account/logout",
            itemNameXpath = "//h1[@class='prod-ProductTitle prod-productTitle-buyBox font-bold']",
            itemPriceXpath = "//span[@class='price display-inline-block arrange-fit price']/span[@class='visuallyhidden']",
            itemButtonXpath = "//button[@data-tl-id='ProductPrimaryCTA-cta_add_to_cart_button']",
            itemButtonXpath2 = "//span[contains(text(), 'Add to cart')]";

        protected override string AddToCartText {
            get {
                return "add to cart";
            }
        }

        public Walmart(string name, WebsiteConfig config, Dictionary<string, Dictionary<string, string>> cred)
            : base(name, config, cred) { }

        protected override Product ParseNoBrowser(string url) {
            log.Information("checking walmart");
            AddToCartButton = null;
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
                //log.Information("\n name:\n" + node.InnerText);
                product.name = node.InnerText == null ? "null" : node.InnerText;
            }
            
            node = doc.DocumentNode.SelectSingleNode(itemPriceXpath);
            if (node != null) {
                //log.Information("\n price:\n" + node.InnerText);
                float number;
                product.price = float.TryParse(node.InnerText, style, culture, out number) ? number : float.MaxValue;
            }
            
            node = doc.DocumentNode.SelectSingleNode(itemButtonXpath2);
            if (node != null) {
                //log.Information("\n button:\n" + node.Attributes["value"].Value);
                //product.button = node.Attributes["value"] == null ? "null" : node.Attributes["value"].Value;
                product.button = node.InnerText == null ? "null" : node.InnerText.ToLower();
            }

            product.link = url;

            return product;
        }

        protected override Product ParseBrowser(string url) {
            log.Information("checking walmart");
            AddToCartButton = null;
            GoToUrl(url);
            Product product = new Product();
            product.link = url;

            IWebElement elem = FindElementTimeout(3, x => driver.FindElementByXPath(x), itemNameXpath);
            if (elem != null) {
                product.name = elem.GetAttribute("innerText").Trim();
            } else {
                if (driver.Url.ToLower().Contains("blocked")) {
                    log.Warning("captcha detected press enter when solved");
                    Console.ReadLine();
                }
            }

            elem = FindElementTimeout(1, x => driver.FindElementByXPath(x), itemPriceXpath);
            if (elem != null) {
                //log.Information("price = [" + elem.GetAttribute("innerText") +  "]");
                float number;
                product.price = float.TryParse(elem.GetAttribute("innerText"), style, culture, out number) ? number : float.MaxValue;
            }

            elem = FindElementTimeout(1, x => driver.FindElementByXPath(x), itemButtonXpath2);
            if (elem != null) {
                log.Information("setting button");
                product.button = elem.GetAttribute("innerText").ToLower();
                AddToCartButton = elem;
            }

            return product;
        }

        protected override bool AddToCart(string url, bool refresh = false) {
            log.Information("adding product to walmart cart");

            if (refresh)
                GoToUrl(url);

            try {
                if (AddToCartButton is null) {

                    AddToCartButton = FindElementTimeout(5, x => driver.FindElementByXPath(x), itemButtonXpath);
                }
                TryInvokeElement(5, () => { AddToCartButton.Click(); });
                WaitUntilStale(10, AddToCartButton, () => { bool b = AddToCartButton.Displayed; });
                return true;
            }
            catch (Exception ex) {
                try {
                    IWebElement captcha = driver.FindElementById("px-captcha");
                    log.Warning("captcha detected.  press Enter when solved");
                    Console.ReadLine();
                    return AddToCart(url);
                } catch (NoSuchElementException e) {
                    log.Warning("no captcha detected");
                }
            }
            return false;
        }

        protected override bool Checkout() {

            log.Information("checkout Walmart");

            GoToUrl(checkoutUrl);

            IWebElement bttn;

            log.Information("searching for continue button");
            try {
                bttn = FindElementTimeout(10, x => driver.FindElementByXPath(x),
                "//span[contains(text(), 'Continue')]");
                TryInvokeElement(5, () => { bttn.Click(); });
                WaitUntilStale(5, bttn, () => { bool b = bttn.Displayed || bttn.Enabled; });
            } catch (Exception ex) {
                log.Information(ex.Message);
                try {
                    IWebElement captcha = driver.FindElementById("px-captcha");
                    log.Warning("captcha detected.  press Enter when solved");
                    Console.ReadLine();
                    return Checkout();
                } catch (Exception e) {
                    log.Warning(e.Message);
                    log.Warning("no captcha found");
                }

            }

            log.Information("searching for next continue button");
            bttn = FindElementTimeout(1, x => driver.FindElementByXPath(x),
                "//button[@class='button button--primary']");
            if (bttn != null) {
                if (TryInvokeElement(5, () => { bttn.Click(); }) != Exceptions.None) return false;

                log.Information("entering cvv");
                bttn = FindElementTimeout(5, x => driver.FindElementById(x), "cvv-confirm");
                if (bttn is null) return false;
                bttn.SendKeys(Keys.Control + "a");
                bttn.SendKeys(paymentInfo["paymentInfo"]["cvv"]);
                bttn.SendKeys(Keys.Enter);
            } else {
                log.Information("continue button not found");
            }


            //bttn = FindElementTimeout(5, x => driver.FindElementByXPath(x),
            //    "//button[@data-automation-id='submit-payment-cc']");
            //bttn.Click();

            bttn = FindElementTimeout(5, x => driver.FindElementByXPath(x),
                "//span[contains(text(), 'Place order')]");
            if (bttn is null) return false;
            if (config.placeOrder) {
                bttn.Click();
                WaitUntilStale(30, bttn, () => { bool b = bttn.Displayed || bttn.Enabled; });
            } else
                log.Information(bttn.GetAttribute("innerText"));

            //# radio = find_element_timeout(5, lambda: driver.find_element(
            //# By.CSS_SELECTOR, "[id^=fulfillment-shipping]"))
            //# radio.click()
            //# bttn = find_element_timeout(5, lambda: driver.find_element(
            //# By.XPATH, '//button[text()="Check out"]'))
            //# bttn.click()
            return true;
        }

        protected override bool Login(string email, string password) {
            log.Information("login to walmart");
            Task.Delay(500).Wait();
            GoToUrl(loginUrl);

            log.Information("entering email");

            IWebElement elem = FindElementTimeout(5, x => driver.FindElementById(x), "email");
            if (elem is null) return false;

            TryInvokeElement(5, () => { elem.SendKeys(Keys.Control + "a"); });
            elem.SendKeys(email);

            log.Information("entering password");

            elem = FindElementTimeout(5, x => driver.FindElementById(x), "password");
            if (elem is null) return false;

            elem.SendKeys(Keys.Control + "a");
            elem.SendKeys(password);

            Task.Delay(500).Wait();

            elem.SendKeys(Keys.Enter);

            WaitUntilStale(5, elem, () => { bool b = elem.Displayed || elem.Enabled; });
            if (driver.Url.ToLower().Contains("account/login") || driver.Url.ToLower().Contains("blocked")) {
                log.Warning("capcha detected press enter when solved");
                Console.ReadLine();
            }
            

            log.Information("Login Successful");
            return true;
        }
    }
}
