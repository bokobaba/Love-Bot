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
    class BestBuy : Website {
        private static readonly string
            cartUrl = "https://www.bestbuy.com/cart",
            checkoutUrl = "https://www.bestbuy.com/checkout/r/fufillment",
            loginUrl = "https://www.bestbuy.com/identity/global/signin",
            itemNameXpath = "//div[@class='sku-title']/h1",
            itemPriceXpath = "//div[@class='priceView-hero-price priceView-customer-price']/span",
            itemButtonXpath = "//button[@class='btn btn-primary btn-lg btn-block btn-leading-ficon add-to-cart-button']";

        public BestBuy(string name, WebsiteConfig config, Dictionary<string, Dictionary<string, string>> cred)
            : base(name, config, cred) {
            config.loadBrowserOnStart = true;
        }

        protected override string AddToCartText {
            get {
                return "add to cart";
            }
        }

        public override void UpdateConfigs(WebsiteConfig newConfig) {
            base.UpdateConfigs(newConfig);
            config.loadBrowserOnStart = true;
        }

        protected override bool AddToCart(string url, bool refresh = false) {
            log.Information("adding product to Bestbuy cart");

            if (refresh) {
                GoToUrl(url);
                AddToCartButton = FindElementTimeout(5, x => driver.FindElementByXPath(x), itemButtonXpath);
            }
            if (AddToCartButton is null) return false;
            TryInvokeElement(5, () => { AddToCartButton.Click(); });

            FindElementTimeout(5, x => driver.FindElementByXPath(x), "div[@class='cart-subtotal'");

            return true;
        }

        protected override bool Checkout() {
            log.Information("checkout Bestbuy");

            GoToUrl(cartUrl);

            log.Information("searching for shipping radio");
            IWebElement elem = FindElementTimeout(5, x => driver.FindElementByXPath(x), "//input[contains(@id, 'shipping')]");
            if (elem is null) return false;
            elem.Click();

            log.Information("searching for checkout button");
            elem = FindElementTimeout(5, x => driver.FindElementByXPath(x),
                "//button[@class='btn btn-lg btn-block btn-primary']");
            if (elem is null) return false;
            elem.Click();
            WaitUntilStale(5, elem, () => { bool b = elem.Displayed || elem.Enabled; });

            elem = FindElementTimeout(2, x => driver.FindElementById(x), "credit-card-cvv");
            if (elem != null) {
                log.Information("entering cvv");
                elem.SendKeys(Keys.Control + "a");
                elem.SendKeys(paymentInfo["paymentInfo"]["cvv"]);
            }

            log.Information("searcing for place order button");
            elem = FindElementTimeout(5, x => driver.FindElementByXPath(x), 
                "//button[@class='btn btn-lg btn-block btn-primary button__fast-track']");
            if (elem is null) return false;
            if (config.placeOrder) {
                if (TryInvokeElement(5, () => {
                    new OpenQA.Selenium.Interactions.Actions(driver).MoveToElement(elem).Click(elem).Perform();
                }) != Exceptions.None) return false;
                WaitUntilStale(30, elem, () => { bool b = elem.Displayed || elem.Enabled; });

            }
            else
                log.Information(elem.GetAttribute("innerText"));

            return true;
        }

        protected override bool Login(string email, string password) {
            log.Information("logging in to Bestbuy");
            //Task.Delay(2000).Wait();
            GoToUrl(loginUrl);

            Task.Delay(1000).Wait();

            log.Information("finding email field");

            IWebElement elem = FindElementTimeout(5, x => driver.FindElementById(x), "fld-e");
            if (elem is null) return false;

            log.Information("entering email");

            elem.SendKeys(Keys.Control + "a");
            elem.SendKeys(email);

            elem = FindElementTimeout(5, x => driver.FindElementById(x), "fld-p1");
            if (elem is null) return false;

            log.Information("entering password");

            elem.SendKeys(Keys.Control + "a");
            elem.SendKeys(password);

            Task.Delay(1000).Wait();

            elem.SendKeys(Keys.Enter);

            WaitUntilStale(10, elem, () => { bool b = elem.Displayed || elem.Enabled; });

            log.Information("login successful");
            return true;
        }

        protected override Product ParseBrowser(string url) {
            log.Information("checking Bestbuy");
            AddToCartButton = null;
            GoToUrl(url);
            Product product = new Product();
            product.link = url;

            IWebElement elem = FindElementTimeout(3, x => driver.FindElementByXPath(x), itemNameXpath);
            if (elem != null) {
                product.name = elem.GetAttribute("innerText").Trim();
            }

            elem = FindElementTimeout(1, x => driver.FindElementByXPath(x), itemPriceXpath);
            if (elem != null) {
                //log.Information("price = [" + elem.GetAttribute("innerText") +  "]");
                float number;
                product.price = float.TryParse(elem.GetAttribute("innerText"), style, culture, out number) ? number : Single.NaN;
            }

            elem = FindElementTimeout(1, x => driver.FindElementByXPath(x), itemButtonXpath);
            if (elem != null) {
                product.button = elem.GetAttribute("innerText").ToLower();
                AddToCartButton = elem;
            }

            return product;
        }

        protected override Product ParseNoBrowser(string url) {
            throw new NotImplementedException();
        }
    }
}
