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
            itemNameXpath = "//div[@class='sku-title']/hl/text()",
            itemPriceXpath = "//div[@class='priceView-hero-price priceView-customer-price']/text()",
            itemButtonXpath = "//input[@class='btn btn-primary btn-lg btn-block btn-leading-ficon add-to-cart-button']/text()";

        public BestBuy(string name, WebsiteConfig config, Dictionary<string, Dictionary<string, string>> cred)
            : base(name, config, cred) { }

        internal override string AddToCartButton {
            get {
                return "Add to cart";
            }
        }

        protected override async Task<Product> ParseAsync(string url) {
            HtmlDocument doc = new HtmlWeb().Load(url);

            //string html = WebsiteUtils.GetHtmlContent(url);

            //HtmlDocument doc = new HtmlDocument();
            //doc.LoadHtml(html);

            //StreamWriter file = new StreamWriter("test.txt");
            //if (doc.DocumentNode != null)
            //    foreach (var n in doc.DocumentNode.ChildNodes)
            //        WebsiteUtils.WriteNode(file, n, 0);

            Product product = new Product();

            HtmlNode node = doc.DocumentNode.SelectSingleNode(itemNameXpath);

            if (node != null) {
                Console.WriteLine("\n name:\n" + node.InnerText);
                product.name = node.InnerText == null ? "null" : node.InnerText;
            }
            else {
                Console.WriteLine("\n name:\nnot found");
                product.name = "not found";
            }

            node = doc.DocumentNode.SelectSingleNode(itemPriceXpath);
            if (node != null) {
                Console.WriteLine("\n price:\n" + node.InnerText);
                NumberStyles style = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;
                CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
                float number;
                product.price = float.TryParse(node.InnerText, style, culture, out number) ? number : float.MaxValue;
            }
            else {
                Console.WriteLine("\nprice:\nnot found");
                product.price = float.MaxValue;
            }

            node = doc.DocumentNode.SelectSingleNode(itemButtonXpath);
            if (node != null) {
                Console.WriteLine("\nbutton:\n" + node.Attributes["value"].Value);
                product.button = node.Attributes["value"] == null ? "null" : node.Attributes["value"].Value;
            }
            else {
                Console.WriteLine("\nbutton: not found\n");
                product.button = "not found";
            }

            product.link = url;

            return product;
        }

        protected override async void AddToCart(ChromeDriver driver, string url) {
            return;
            Console.WriteLine("adding product to walmart cart");
            Console.WriteLine(url);
            Task.Delay(3000).Wait();
            driver.Navigate().GoToUrl(url);
            //Console.ReadLine();

            IWebElement bttn = FindElementTimeout(5, x => driver.FindElementByXPath(x),
                "//button[@class='button spin-button prod-ProductCTA--primary button--primary']");
            bttn.Click();
        }

        protected override async void Checkout(ChromeDriver driver) {
            return;
            Task.Delay(2000).Wait();

            Console.WriteLine("proceding to cart");

            driver.Navigate().GoToUrl(cartUrl);

            new WebDriverWait(driver, TimeSpan.FromSeconds(10)).Until(ExpectedConditions.ElementToBeClickable((
                By.XPath("//button[@class='button cxo-continue-btn button--primary']"))));
            IWebElement bttn = FindElementTimeout(5, x => driver.FindElementByXPath(x),
                "//button[@class='button cxo-continue-btn button--primary']");
            bttn.Click();

            new WebDriverWait(driver, TimeSpan.FromSeconds(10)).Until(ExpectedConditions.ElementToBeClickable((
                By.XPath("//button[@class='button button--primary']"))));
            bttn = FindElementTimeout(5, x => driver.FindElementByXPath(x),
                "//button[@class='button button--primary']");
            bttn.Click();

            //bttn = FindElementTimeout(5, x => driver.FindElementByXPath(x),
            //    "//span[contains(text(), 'Review your order')]");
            //bttn.Click();

            bttn = FindElementTimeout(5, x => driver.FindElementById(x),
                "cvv-confirm");
            bttn.SendKeys(Keys.Control + "a");
            bttn.SendKeys("488");


            bttn = FindElementTimeout(5, x => driver.FindElementByXPath(x),
                "//button[@data-automation-id='submit-payment-cc']");
            bttn.Click();

            bttn = FindElementTimeout(5, x => driver.FindElementByXPath(x),
                "//button[@data-automation-id='summary-place-holder']");
            bttn.Click();

            Console.WriteLine("bttn text: " + bttn.Text);
            Console.WriteLine("success");


            //# radio = find_element_timeout(5, lambda: driver.find_element(
            //# By.CSS_SELECTOR, "[id^=fulfillment-shipping]"))
            //# radio.click()
            //# bttn = find_element_timeout(5, lambda: driver.find_element(
            //# By.XPATH, '//button[text()="Check out"]'))
            //# bttn.click()
        }

        protected override async void Login(ChromeDriver driver, string email, string password) {
            Console.WriteLine("login to walmart");
            Task.Delay(2000).Wait();
            Console.WriteLine("going to login url");
            driver.Navigate().GoToUrl(loginUrl);

            Task.Delay(1000).Wait();

            Console.WriteLine("finding email field");

            IWebElement elem = FindElementTimeout(5, x => driver.FindElementById(x), "email");

            //elem = find_element_timeout(5, lambda: driver.find_element_by_id('email'),
            //                            'unable to find email element for login', False)

            Console.WriteLine("entering email");

            elem.SendKeys(Keys.Control + "a");
            elem.SendKeys(email);

            elem = FindElementTimeout(5, x => driver.FindElementById(x), "password");

            //elem = find_element_timeout(5, lambda: driver.find_element_by_id('password'),
            //                            'unable to find password element for login', False)

            Console.WriteLine("entering password");

            elem.SendKeys(Keys.Control + "a");
            elem.SendKeys(password);

            Task.Delay(1000).Wait();

            elem.SendKeys(Keys.Enter);

            Console.WriteLine("success");
        }
    }
}
