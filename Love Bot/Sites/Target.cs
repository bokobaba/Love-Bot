﻿using HtmlAgilityPack;
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
            checkoutUrl = "https://www.target.com/co-payment",
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
            log.Information("adding product to Target cart");
            if (AddToCartButton is null) {
                GoToUrl(url);

                AddToCartButton = FindElementTimeout(5, x => driver.FindElementByXPath(x), itemButtonXpath);
                if (AddToCartButton is null) return false;
            }

            log.Information("clicking ship it button");
            if (TryInvokeElement(5, () => { AddToCartButton.Click(); }) != Exceptions.None)
                return false;

            FindElementTimeout(5, x => driver.FindElementByXPath(x), "//div[@class='ReactModal__Overlay ReactModal__Overlay--after-open']");

            return true;
        }

        protected override bool Checkout() {
            log.Information("checkout Target");

            GoToUrl(checkoutUrl);

            //log.Information("searching for save and continue button");
            //IWebElement e = FindElementTimeout(5, x => driver.FindElementByXPath(x), "//button[@data-test='save-and-continue-button");
            //if (e is null) return false;
            //if (TryInvokeElement(5, () => { e.Click(); }) != Exceptions.None) return false;

            log.Information("entering cvv");
            IWebElement e = FindElementTimeout(5, x => driver.FindElementById(x), "creditCardInput-cvv");
            if (e != null) {
                if (TryInvokeElement(5, () => { e.SendKeys(Keys.Control + "a"); }) != Exceptions.None) return false;
                e.SendKeys(paymentInfo["paymentInfo"]["cvv"]);

                log.Information("searching for save and continue button");
                e = FindElementTimeout(5, x => driver.FindElementByXPath(x), "//button[contains(text(), 'Save and continue')]");
                if (e is null) return false;
                if (TryInvokeElement(5, () => { e.Click(); }) != Exceptions.None) return false;
            }

            log.Information("searching for credit card confirm");
            e = FindElementTimeout(1, x => driver.FindElementById(x), "creditCardInput-cardnumber");
            if (e != null) {
                e.SendKeys(Keys.Control + "a");
                e.SendKeys(paymentInfo["paymentInfo"]["creditCardNum"]);
                e.SendKeys(Keys.Enter);
            } else {
                log.Information("credit card confirm not found");
            }

            
            log.Information("searching for place order button");
            e = FindElementTimeout(5, x => driver.FindElementByXPath(x), "//button[contains(text(), 'Place your order')]");
            if (e is null) return false;

            if (config.placeOrder) {
                if (TryInvokeElement(5, () => { e.Click(); }) != Exceptions.None) return false;
                if (!WaitUntilStale(20, e, () => { bool b = e.Displayed || e.Enabled; })) return false;
            } else {
                log.Information(e.GetAttribute("innerText"));
            }

            return true;
        }

        protected override bool Login(string email, string password) {
            log.Information("logging in to Target");
            GoToUrl(loginUrl);

            log.Information("searching for account header");
            IWebElement elem = FindElementTimeout(5, x => driver.FindElementByXPath(x), "//span[@data-test='accountUserName']");
            if (elem is null) return false;
            elem.Click();
            if (!elem.GetAttribute("innerText").Equals("Sign in")) {
                log.Information("signing out");
                elem = FindElementTimeout(5, x => driver.FindElement(By.Id(x)), "accountNav-guestSignOut");
                if (elem is null) return false;
                Task.Delay(2000).Wait();
                elem.Click();
                WaitUntilStale(5, elem, () => { bool b = elem.Displayed || elem.Enabled; });
                Login(email, password);
            }

            log.Information("searching for signin button"); 
            elem = FindElementTimeout(5, x => driver.FindElement(By.Id(x)), "accountNav-signIn");
            if (elem is null) return false;
            log.Information("trying to click");
            if (TryInvokeElement(5, () => { elem.Click(); }) != Exceptions.None)
                return false;
            if (!WaitUntilStale(10, elem, () => { bool b = elem.Displayed || elem.Enabled; }))
                return false;

            log.Information("entering email");

            elem = FindElementTimeout(5, x => driver.FindElementById(x), "username");
            if (elem != null) {
                TryInvokeElement(5, () => { elem.SendKeys(Keys.Control + "a"); });
                elem.SendKeys(email);
            } else {
                log.Information("no username field found");
            }

            log.Information("entering password");

            elem = FindElementTimeout(5, x => driver.FindElementById(x), "password");
            if (elem is null) return false;

            elem.SendKeys(Keys.Control + "a");
            elem.SendKeys(password);

            Task.Delay(500).Wait();

            elem.SendKeys(Keys.Enter);

            WaitUntilStale(5, elem, () => { bool b = elem.Displayed || elem.Enabled; });


            log.Information("Login Successful");
            return true;
        }

        protected override Product ParseBrowser(string url) {
            log.Information("checking Target");
            AddToCartButton = null;
            GoToUrl(url);
            Product product = new Product();
            product.link = url;

            Task.Delay(2000).Wait();
            IWebElement elem = FindElementTimeout(3, x => driver.FindElementByXPath(x), itemNameXpath);
            if (elem != null) {
                TryInvokeElement(5, () => { product.name = elem.GetAttribute("innerText").Trim(); });
            }

            elem = FindElementTimeout(1, x => driver.FindElementByXPath(x), itemPriceXpath);
            if (elem != null) {
                //log.Information("price = [" + elem.GetAttribute("innerText") +  "]");
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
