using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Love_Bot.Sites {
    class Gamestop : Website {
        private static readonly string
            logouturl = "https://www.gamestop.com/logout/",
            loginUrl = "http://gamestop.com/?openLoginModal=accountModal",
            cartUrl = "https://www.gamestop.com/cart/",
            checkouturl = "https://www.gamestop.com/checkout/?stage=payment#payment",
            itemnameXpath = "//h1[@class='product-name h2']",
            itemPriceXpath = "//span[@class='actual-price']",
            itemButtonXpath = "//button[@class='add-to-cart btn btn-primary ']";

        public Gamestop(string name, WebsiteConfig config, Dictionary<string, Dictionary<string, string>> cred)
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
            Console.WriteLine(name + ": adding product to gamestop cart");
            if (AddToCartButton is null) {
                driver.Navigate().GoToUrl(url);

                IWebElement elem = FindElementTimeout(5, x => driver.FindElementByXPath(x), itemButtonXpath);
                if (elem is null) return false;
                elem.Click();
            } else {
                if (TryInvokeElement(5, () => { AddToCartButton.Click(); }) != Exceptions.None)
                    return false;
            }


            //Thread.Sleep(500);
            //if (!WaitUntilStale(10, elem, () => { bool b = elem.Enabled || elem.Displayed; }))
            //    return false;

            FindElementTimeout(5, x => driver.FindElementById(x), "addedToCartModal");
            

            //elem.Click();

            AddToCartButton = null;
            return true;
        }

        protected override bool Checkout() {
            Console.WriteLine(name + ": checkout gamestop");
            driver.Navigate().GoToUrl(checkouturl);
            //IWebElement e = FindElementTimeout(5, x => driver.FindElementById(x), "shippingAddressOne");
            //if (e is null) return false;
            //e.SendKeys(Keys.Control + "a");
            //e.SendKeys(paymentInfo["shippingInfo"]["address"]);

            //e = FindElementTimeout(5, x => driver.FindElementById(x), "shippingAddressTwo");
            //if (e is null) return false;
            //e.SendKeys(Keys.Control + "a");
            //e.SendKeys(paymentInfo["shippingInfo"]["address2"]);

            //e = FindElementTimeout(5, x => driver.FindElementById(x), "shippingAddressCity");
            //if (e is null) return false;
            //e.SendKeys(Keys.Control + "a");
            //e.SendKeys(paymentInfo["shippingInfo"]["city"]);

            //e = FindElementTimeout(5, x => driver.FindElementById(x), "shippingZipCode");
            //if (e is null) return false;
            //e.SendKeys(Keys.Control + "a");
            //e.SendKeys(paymentInfo["shippingInfo"]["zipcode"]);

            //SelectElement s = new SelectElement(FindElementTimeout(5, x => driver.FindElementById(x), "shippingState"));
            //if (e is null) return false;
            //s.SelectByText(WebsiteUtils.states[paymentInfo["shippingInfo"]["state"]]);

            //IWebElement e = FindElementTimeout(5, x => driver.FindElementByClassName(x), "next-step-summary-button");
            //if (e is null) return false;
            //IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            //js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight)");
            //Thread.Sleep(2000);
            //Console.WriteLine("trying to click");
            //Console.WriteLine("enabled: " + e.Enabled + " visible: " + e.Displayed);
            //Console.ReadLine();
            //if (TryInvokeElement(10, () => {

            //    e.Click();
            //}) != Exceptions.None) return false;

            IWebElement e = FindElementTimeout(5, x => driver.FindElementById(x), "saved-payment-security-code");
            if (e is null) return false;
            Console.WriteLine(name + ": entering cvv");
            if (TryInvokeElement(5, () => { e.SendKeys(Keys.Control + "a"); }) != Exceptions.None) return false;
            e.SendKeys(paymentInfo["paymentInfo"]["cvv"]);

            e = FindElementTimeout(5, x => driver.FindElementByXPath(x), "//button[@class='btn btn-primary btn-block submit-payment']");
            if (e is null) return false;
            if (TryInvokeElement(5, () => {
                new OpenQA.Selenium.Interactions.Actions(driver).MoveToElement(e).Click(e).Perform();
            }) != Exceptions.None) return false;

            Console.WriteLine(name + ": searching for place order button");
            e = FindElementTimeout(5, x => driver.FindElementByXPath(x), "//button[@class='btn btn-primary btn-block place-order']");
            if (e is null) return false;
            if (config.placeOrder) {
                if (TryInvokeElement(5, () => {
                    new OpenQA.Selenium.Interactions.Actions(driver).MoveToElement(e).Click(e).Perform();
                }) != Exceptions.None) return false;
                WaitUntilStale(30, e, () => { bool b = e.Displayed || e.Enabled; });
            } else
                Console.WriteLine(e.GetAttribute("value"));

            

            return true;
        }

        protected override bool Login(string email, string password) {
            Console.WriteLine("logging into gamestop");
            driver.Navigate().GoToUrl(logouturl);
            Task.Delay(2000).Wait();
            driver.Navigate().GoToUrl(loginUrl);

            //IWebElement elem = FindElementTimeout(10, x => driver.FindElementByCssSelector(x), "[href='#accountModal'");
            //if (elem is null) return false;
            //elem.Click();

            IWebElement elem = FindElementTimeout(10, x => driver.FindElementById(x), "signIn");
            if (elem is null) return false;
            Exceptions ex = TryInvokeElement(10, () => { elem.Click(); });

            Console.WriteLine(name + ": searching for email field");
            elem = FindElementTimeout(5, x => driver.FindElementById(x), "login-form-email");
            if (elem is null) return false;
            ex = TryInvokeElement(10, () => { elem.SendKeys(Keys.Control + "a"); });
            elem.SendKeys(email);

            Console.WriteLine(name + ": searching for password field");
            elem = FindElementTimeout(5, x => driver.FindElementById(x), "login-form-password");
            if (elem is null) return false;
            elem.SendKeys(Keys.Control + "a");
            elem.SendKeys(password);
            Thread.Sleep(1000);
            elem.SendKeys(Keys.Enter);

            if (!WaitUntilStale(10, elem, () => { bool b = elem.Displayed; }))
                return false;

            Console.WriteLine(name + ": login successful");
            return true;
        }

        protected override Product ParseBrowser(string url) {
            Console.WriteLine(name + ": checking gamestop");
            AddToCartButton = null;
            driver.Navigate().GoToUrl(url);
            Product product = new Product();
            product.link = url;

            IWebElement elem = FindElementTimeout(3, x => driver.FindElementByXPath(x), itemnameXpath);
            if (elem != null) {
                product.name = elem.GetAttribute("innerText").Trim();
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
