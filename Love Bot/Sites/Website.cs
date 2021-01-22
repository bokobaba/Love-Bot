using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Love_Bot.Sites {
    abstract class Website {
        private int purchases = 0;
        private bool searching = false;
        private bool loggingin = false;
        public Task searchTask, loginTask;

        public string name;
        public WebsiteConfig config;
        public Dictionary<string, Dictionary<string, string>> paymentInfo;
        public CancellationTokenSource abort;
        //public bool abort = false;

        protected ChromeDriver driver;
        protected NumberStyles style = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;
        protected CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
        protected IWebElement AddToCartButton;

        protected abstract string AddToCartText {
            get;
        }

        protected static readonly List<string> userAgents = new List<string>() {
            "Mozilla/5.0 (X11; Ubuntu; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2919.83 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.157 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36",
            "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2490.71 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.100 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.88 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.108 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.2; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.90 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.130 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.105 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.121 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.116 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.138 Safari/537.36"
        };


        protected Website(string name, WebsiteConfig config, Dictionary<string, Dictionary<string, string>> payment) {
            this.name = name;
            this.config = config;
            paymentInfo = payment;
        }

        public class Product {

            public string name { get; set; } = "not found";
            public float price { get; set; } = Single.NaN;
            public string button { get; set; } = "not found";
            public string link { get; set; }

            public override string ToString() {
                string v = $"Product:\nname: {name}\nprice: {price}\nbuy button: {button}\nlink:{link}";
                return v;
            }
        }

        protected enum Exceptions {
            None,
            ElementClickIntercepted,
            NotInteractable,
            StaleElement,
            NoSuch,
            Timeout
        }

        public virtual void UpdateConfigs(WebsiteConfig newConfig) {
            Console.WriteLine(name + ": updating config");
            config.Update(newConfig);
        }

        public async Task Run() {
            Console.WriteLine(name + " is running");
            if (config.urls.Length < 1) {
                Console.WriteLine(name + ": no urls");
                End();
            }
            if (config.loadBrowserOnStart) {
                driver = InitDriver();
                //driver.Navigate().GoToUrl(config.urls[0]);
            }
            if (driver != null && config.stayLoggedIn)
                Login(config.login[0], config.login[1]);

            Task.Run(() => startTasks()).Wait();
        }

        private async Task startTasks() {
            List<Task> tasks = new List<Task>();
            //Task searchTask = Task.Run(async () => { 
            //    await Task.Delay(TimeSpan.FromSeconds(config.delay)); 
            //});
            //Task loginTask = Task.Run(async () => { await Task.Delay(TimeSpan.FromSeconds(config.loginInterval)); });
            searchTask = Search();
            loginTask = StayLoggedIn();

            CancellationTokenSource tokenSource = new CancellationTokenSource();


            await searchTask;
            await loginTask;

            //while (!abort) {
            //    Task finished = await Task.WhenAny(tasks);
            //    tasks.Remove(finished);
            //    if (finished.Id == ids.Item1) {
            //        Task task = Search();
            //        //while (searching || loggingin) { }
            //        //Search();
            //        //Task task = Task.Run(async () => { await Task.Delay(TimeSpan.FromSeconds(config.delay)); });
            //        ids.Item1 = task.Id;
            //        tasks.Add(task);
            //    }
            //    else if (finished.Id == ids.Item2) {
            //        Task task = StayLoggedIn();
            //        //while (loggingin || searching) { }
            //        //StayLoggedIn();
            //        //Task task = Task.Run(async () => { await Task.Delay(TimeSpan.FromSeconds(config.loginInterval)); });
            //        ids.Item2 = task.Id;
            //        tasks.Add(task);
            //    }
            //}
            End();
        }

        private void End() {
            Console.WriteLine("bot: " + name + " has exited");
        }

        private async Task Search() {
            //if (abort) return;
            while (!abort.IsCancellationRequested) {
                //Console.WriteLine("starting search: " + DateTime.Now);
                await Task.Delay(TimeSpan.FromSeconds(config.delay), abort.Token).ContinueWith(tsk => { });
                //Console.WriteLine("ending search: " + DateTime.Now);
                while ((searching || loggingin) && !abort.IsCancellationRequested) { }
                searching = true;
                foreach (string url in config.urls) {
                    if (abort.IsCancellationRequested) break;
                    Product product = driver is null ? ParseNoBrowser(url) : ParseBrowser(url);
                    if (VerifyProduct(product)) {
                        PurchaseItem(product);
                        if (purchases < config.maxPurchases) {
                            Console.WriteLine(name + ": restarting browser");
                            driver.Dispose();
                            driver = InitDriver();
                            if (config.stayLoggedIn)
                                Login(config.login[0], config.login[1]);
                        }
                    }

                    if (config.maxPurchases > 0 && purchases >= config.maxPurchases) {
                        searching = false;
                        driver.Dispose();
                        driver = null;
                        return;
                    }
                }
                searching = false;
            }
        }

        private async Task StayLoggedIn() {
            //if (abort) return;
            while (!abort.IsCancellationRequested) {
                //Console.WriteLine("starting login: " + DateTime.Now);
                await Task.Delay(TimeSpan.FromSeconds(config.loginInterval), abort.Token).ContinueWith(tsk => { });
                //Console.WriteLine("ending login: " + DateTime.Now);
                while ((searching || loggingin) && !abort.IsCancellationRequested) { }
                if (abort.IsCancellationRequested) break;
                loggingin = true;
                if (driver != null && config.stayLoggedIn) {
                    AddToCartButton = null;
                    Login(config.login[0], config.login[1]);
                }
                loggingin = false;
            }
        }

        private ChromeDriver InitDriver() {
            Console.WriteLine(name + ": starting browser");
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--ignore-certificate-errors-spki-list");
            options.AddArgument("--ignore-ssl-errors");

            options.AddAdditionalCapability("useAutomationExtension", false);
            //options.AddExcludedArgument("enable-automation");
            options.AddExcludedArguments(new List<string>() { "enable-automation" });
            options.AddArgument("--disable-blink-features");
            options.AddArguments("--disable-blink-features=AutomationControlled");
            //options.AddArguments("--disable-dev-shm-usage");
            options.PageLoadStrategy = PageLoadStrategy.Eager;
            if (config.headless)
                options.AddArgument("headless");
            //options.AddArguments("user-data-dir=" + AppDomain.CurrentDomain.BaseDirectory + @"\Profile");
            //options.AddUserProfilePreference("profile.managed_default_content_settings.images", 0);

            Random r = new Random();
            string agent = userAgents[r.Next(0, userAgents.Count - 1)];
            Console.WriteLine(name + ": useragent = " + agent);
            options.AddArgument("--user-agent=" + agent);

            options.Proxy = null;

            string loc = System.IO.Directory.GetCurrentDirectory();
            //loc = loc.Substring(0, loc.LastIndexOf(@"\") + 1);
            Console.WriteLine("current directory = " + loc);
            //Console.ReadLine();

            ChromeDriver driver = new ChromeDriver(loc, options);
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(10);
            

            return driver;
        }

        protected bool VerifyProduct(Product product) {
            Console.WriteLine(name + ": verifying: " + product);
            if (!AddToCartText.Contains(product.button)) {
                Console.WriteLine(name + ": purchase button not found");
                return false;
            }
            if (config.maxPrice > 0 && (product.price > config.maxPrice || float.IsNaN(product.price))) {
                Console.WriteLine(name + ": price match failed");
                return false;
            }
            Console.WriteLine(name + ": product match");
            return true;
        }

        private void PurchaseItem(Product product) {
            Console.WriteLine(name + ": trying to purchase");
            int attempts = 1;
            Stopwatch watch = new Stopwatch();
            watch.Start();

            if (driver is null)
                driver = InitDriver();

            Console.WriteLine(name + ": purchase attempt " + attempts++);
        
            if (!config.stayLoggedIn) {
                AddToCartButton = null;
                while (!Login(config.login[0], config.login[1])) {
                    if (abort.IsCancellationRequested) return;
                    Console.WriteLine(name + ": login failed");
                    if (attempts - 1 > config.checkoutAttempts)
                        return;
                    Console.WriteLine(name + ": purchase attempt " + attempts++);
                }
            }

            int refresh = attempts;
            while (!AddToCart(product.link, AddToCartButton is null || attempts > refresh)) {
                if (abort.IsCancellationRequested) return;
                AddToCartButton = null;
                Console.WriteLine(name + ": add to cart failed");
                if (attempts - 1 > config.checkoutAttempts)
                    return;
                Console.WriteLine(name + ": purchase attempt" + attempts++);
            }

            while (!Checkout()) {
                if (abort.IsCancellationRequested) return;
                Console.WriteLine(name + ": checkout failed");
                if (attempts - 1 > config.checkoutAttempts)
                    return;
                Console.WriteLine(name + ": purchase attempt " + attempts++);
            }

            Console.WriteLine(name + ": product checkout success");

            purchases++;

            //if (config.stayLoggedIn || Login(config.login[0], config.login[1])) {
            //    while (attempts < 5 && !abort) {
            //        Console.WriteLine(name + ": checkout attempt: " + ++attempts);
            //        while (!AddToCart(product.link, attempts++ > 1)) {
            //            while (!Checkout()) {
            //                Console.WriteLine(name + ": product checkout success");
            //                ++purchases;
            //                break;
            //            }
            //        }
            //    }
            //}
            watch.Stop();
            TimeSpan ts = watch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Console.WriteLine(name + ": Checkout time: " + elapsedTime);
        }


        protected static IWebElement FindElementTimeout(float t, Func<string, IWebElement> func, string s) {
            IWebElement elem = null;
            Stopwatch w = new Stopwatch();
            w.Start();
            while (w.Elapsed.TotalSeconds < t) {
                try {
                    elem = func(s);
                    break;
                } catch (StaleElementReferenceException) {
                    break;
                } catch (NoSuchElementException) {
                    //Console.WriteLine(s + ": no such element exception");
                } catch (Exception ex) {
                    //Console.WriteLine(s + ex);
                }

            }
            return elem;
        }

        protected static Exceptions TryInvokeElement(float t, Action check) {
            Stopwatch w = new Stopwatch();
            w.Start();
            while (w.Elapsed.TotalSeconds < t) {
                try {
                    check.Invoke();
                    return Exceptions.None;
                } catch (ElementClickInterceptedException) {
                    return Exceptions.ElementClickIntercepted;
                } catch (ElementNotInteractableException) {
                    //Console.WriteLine("not interactable");
                    continue;
                } catch (NoSuchElementException) {
                    continue;
                } catch (StaleElementReferenceException) {
                    continue;
                }
            }
            return Exceptions.Timeout;
        }

        protected static bool WaitUntilStale(float t, IWebElement element, Action check) {
            Stopwatch w = new Stopwatch();
            w.Start();
            while (w.Elapsed.TotalSeconds < t) {
                try {
                    check.Invoke();
                } catch (StaleElementReferenceException) {
                    return true;
                } catch (ElementNotInteractableException) {
                    return true;
                }
            }

            return false;
        }

        protected abstract Product ParseNoBrowser(string url);
        protected abstract Product ParseBrowser(string url);
        protected abstract bool AddToCart(string url, bool refresh = false);
        protected abstract bool Checkout();
        protected abstract bool Login(string email, string password);
    }
}
