using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Love_Bot.Sites {
    class Amazon : Website {
        private static readonly string
            cartUrl = "https://www.amazon.com/gp/cart/view.html?ref_=nav_cart",
            loginUrl = "https://www.amazon.com/",
            checkouturl = "https://www.amazon.com/gp/buy/spc/handlers/display.html?hasWorkingJavascript=1",
            itemNameXpath = "//span[@id='productTitle']",
            itemNameId = "productTitle",
            itemPriceXpath = "//span[@id='newBuyBoxPrice']",
            itemPriceId = "newBuyBoxPrice",
            itemButtonXpath = "//input[@id='add-to-cart-button']",
            itemButtonId = "add-to-cart-button",
            buyNowXpath = "//input[@id='buy-now-button']",
            buyNowId = "buy-now-button";

        private bool buyNow = false;

        public Amazon(string name, WebsiteConfig config, Dictionary<string, Dictionary<string, string>> cred)
            : base(name, config, cred) { }

        protected override string AddToCartText {
            get {
                return "Add to Cart, Buy Now";
            }
        }

        protected override bool AddToCart(string url, bool refresh = false) {
            Console.WriteLine(name + ": adding product to amazon cart");

            if (refresh)
                driver.Navigate().GoToUrl(url);

            if (AddToCartButton is null) {
                AddToCartButton = FindElementTimeout(5, x => driver.FindElementById(x),
                    buyNow ? "buy-now-button" : "add-to-cart-button");
                if (AddToCartButton is null)
                    return false;
            }

            if (TryInvokeElement(5, () => { AddToCartButton.Click(); }) != Exceptions.None)
                return false;

            if (buyNow)
                return true;

            IWebElement bttn = FindElementTimeout(5, x => driver.FindElementById(x), "hlb-ptc-btn-native");
            if (bttn is null) return false;
            bttn.Click();

            Console.ReadLine();
            return true;

            //IWebElement elem;
            //if (buyNow) {
            //    Console.WriteLine(name + ": Buy Now button detected");
            //    if (AddToCartButton is null) {
            //        elem = FindElementTimeout(20, x => driver.FindElementById(x), "buy-now-button");
            //        if (elem != null) {
            //            elem.Click();
            //            return true;
            //        }
            //    }
            //}

            //elem = FindElementTimeout(5, x => driver.FindElementById(x), "add-to-cart-button");
            //if (elem is null) return false;
            //elem.Click();

            //if (TryInvokeElement(5, () => { AddToCartButton.Click(); }) != Exceptions.None)
            //    return false;


            //IWebElement bttn = FindElementTimeout(5, x => driver.FindElementById(x), "hlb-ptc-btn-native");
            //if (bttn is null) return false;
            //bttn.Click();

            //return true;
        }
        
        protected override bool Checkout() {
            Console.WriteLine("checkout");
            if (buyNow) {
                IReadOnlyCollection<IWebElement> popover = driver.FindElements(By.Id("a-popover-lgtbox"));
                if (popover.Count > 0) {
                    Console.WriteLine("popup detected");
                    IWebElement iframe = FindElementTimeout(5, x => driver.FindElement(By.Id(x)),
                    "turbo-checkout-iframe");
                    if (iframe is null) return false;
                    driver.SwitchTo().Frame(iframe);
                    IWebElement e = FindElementTimeout(5, x => driver.FindElement(By.Id(x)),
                        "turbo-checkout-pyo-button");
                    if (e is null) return false;

                    if (config.placeOrder)
                        e.Click();
                    else
                        Console.WriteLine(e.GetAttribute("value"));
                    return true;
                }
                else
                    return false;
            }

            driver.Navigate().GoToUrl(checkouturl);

            IWebElement elem = FindElementTimeout(20, x => driver.FindElementByTagName(x), "fieldset");
            if (elem != null) {
                IReadOnlyCollection<IWebElement> radios = elem.FindElements(By.TagName("div"));
                for (int i = 1; i < radios.Count; ++i) {
                    IWebElement e = radios.ElementAt(i).FindElement(By.CssSelector("span[class='a-color-secondary']"));
                    if (elem is null) continue;
                    Console.WriteLine(e.Text);
                    if (e.Text.Contains("FREE") && !e.Text.Contains("trial")) {
                        radios.ElementAt(i).Click();
                        break;
                    }
                }
            }

            elem = FindElementTimeout(20, x => driver.FindElementById(x), "placeYourOrder");
            if (elem is null) return false;
            if (config.placeOrder)
                elem.Click();

            Task.Delay(10000).Wait();
            return true;
        }

        protected override bool Login(string email, string password) {
            Console.WriteLine(name + ": logging to amazon");
            driver.Navigate().GoToUrl(loginUrl);

            IWebElement elem = FindElementTimeout(10, x => driver.FindElementById(x), "nav-link-accountList-nav-line-1");
            if (elem is null)
                return false;

            if (!elem.GetAttribute("innerText").ToLower().Contains("sign in")) {
                Console.WriteLine(name + ": trying to sign out of amazon");
                Actions action = new Actions(driver);
                action.MoveToElement(elem).Perform();

                IWebElement signoutButton = FindElementTimeout(10, x => driver.FindElementById(x), "nav-item-signout");
                if (TryInvokeElement(10, () => { signoutButton.Click(); }) != Exceptions.None)
                    return false;
            }
            else {
                Console.WriteLine(name + ": trying to sign in to amazon");
                if (TryInvokeElement(10, () => { elem.Click(); }) != Exceptions.None)
                    return false;
            }

            //Console.ReadLine();
            Task.Delay(1000).Wait();
            Console.WriteLine(name + ": searching for email field");

            elem = FindElementTimeout(1, x => driver.FindElementById(x), "ap_email");

            //elem = find_element_timeout(5, lambda: driver.find_element_by_id('email'),
            //                            'unable to find email element for login', False)

            if (elem != null) {

                elem.SendKeys(Keys.Control + "a");
                elem.SendKeys(email);
                //Thread.Sleep(2000);
                elem.SendKeys(Keys.Enter);
            }

            //elem = FindElementTimeout(5, x => driver.FindElementById(x), "continue");
            //elem.Click();

            Task.Delay(1000).Wait();
            Console.WriteLine(name + ": searching for password field");
            elem = FindElementTimeout(5, x => driver.FindElementById(x), "ap_password");
            if (elem is null) return false;

            //elem = find_element_timeout(5, lambda: driver.find_element_by_id('password'),
            //                            'unable to find password element for login', False)

            Console.WriteLine("entering password");

            elem.SendKeys(Keys.Control + "a");
            elem.SendKeys(password);

            Task.Delay(1000).Wait();

            elem.SendKeys(Keys.Enter);

            Console.WriteLine("Login Successful");

            return true;
        }

        protected override Product ParseNoBrowser(string url) {
            Console.WriteLine(name + "checking amazon");
            buyNow = false;
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

            if (node != null)
                product.name = node.InnerText == null ? "null" : node.InnerText.Trim();

            node = doc.DocumentNode.SelectSingleNode(itemPriceXpath);
            if (node != null) {
                //Console.WriteLine("\n price:\n" + node.InnerText);
                float number;
                product.price = float.TryParse(node.InnerText, style, culture, out number) ? number : float.MaxValue;
            }

            if ((doc.DocumentNode.SelectSingleNode(buyNowXpath)) == null) {
                node = doc.DocumentNode.SelectSingleNode(itemButtonXpath);
                if (node != null) {
                    //Console.WriteLine(node.InnerText);
                    //Console.WriteLine("\n button:\n" + node.Attributes["value"].Value);
                    product.button = node.Attributes["value"] == null ? "null" : node.Attributes["value"].Value;
                    //product.button = node.InnerText == null ? "null" : node.InnerText;
                }
            }
            else {
                //Console.WriteLine(node.InnerText);
                product.button = "Buy Now";
                buyNow = true;
            }

            product.link = url;

            return product;
        }

        protected override Product ParseBrowser(string url) {
            Console.WriteLine(name + ": checking amazon");
            buyNow = false;
            AddToCartButton = null;
            driver.Navigate().GoToUrl(url);
            Product product = new Product();
            product.link = url;

            IWebElement elem = FindElementTimeout(3, x => driver.FindElementById(x), itemNameId);
            if (elem != null) {
                product.name = elem.GetAttribute("innerText").Trim();
            }

            elem = FindElementTimeout(1, x => driver.FindElementById(x), itemPriceId);
            if (elem != null) {
                //Console.WriteLine("price = [" + elem.GetAttribute("innerText") +  "]");
                float number;
                product.price = float.TryParse(elem.GetAttribute("innerText"), style, culture, out number) ? number : float.MaxValue;
            }

            if ((elem = FindElementTimeout(1, x => driver.FindElementById(x), buyNowId)) == null) {
                elem = FindElementTimeout(1, x => driver.FindElementById(x), itemButtonId);
                if (elem != null) {
                    product.button = elem.Text.ToLower();
                    AddToCartButton = elem;
                }
            }
            else {
                product.button = elem.Text.ToLower();
                AddToCartButton = elem;
                buyNow = true;
            }

            return product;
        }
    }
}
