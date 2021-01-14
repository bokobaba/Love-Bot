using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Love_Bot {
    class WebsiteConfig {
        public string[] login { get; set; } = new string[] { "username", "password" };
        public int checkoutAttempts { get; set; } = 5;
        public int delay { get; set; } = 0;
        public bool placeOrder { get; set; } = false;
        public bool headless { get; set; } = false;
        public bool loadBrowserOnStart { get; set; } = false;
        public bool stayLoggedIn { get; set; } = false;
        public int loginInterval { get; set; } = 600;
        public float maxPrice { get; set; } = 0;
        public float maxPurchases { get; set; } = 0;
        public string[] urls { get; set; } = new string[] { "www.example.com/product_url", "www.example.com/product2_url" };

        public override string ToString() {
            string s = "\tlogin : \n\t\t" + login[0] + "\n\t\t" + login[1] + "\n";
            s += "\tcheckoutAttempts: " + checkoutAttempts + "\n";
            s += "\tloadBrowserOnStart: " + loadBrowserOnStart + "\n";
            s += "\tstayLoggedIn: " + stayLoggedIn + "\n";
            s += "\tloginInterval: " + loginInterval + "\n";
            s += "\theadless: " + headless + "\n";
            s += "\tdelay: " + delay + "\n";
            s += "\tplaceOrder: " + placeOrder + "\n";
            s += "\tmaxPrice: " + maxPrice + "\n";
            s += "\tmaxPurchases: " + maxPurchases + "\n";
            s += "\turls: [" + "\n";
            foreach (string url in urls) {
                s += "\t\t" + url + "\n";
            }
            s += "\t]\n";

            return s;
        }

        public void Update(WebsiteConfig c) {
            login = c.login;
            checkoutAttempts = c.checkoutAttempts;
            delay = c.delay;
            placeOrder = c.placeOrder;
            headless = c.headless;
            loadBrowserOnStart = c.loadBrowserOnStart;
            stayLoggedIn = c.stayLoggedIn;
            loginInterval = c.loginInterval;
            maxPrice = c.maxPrice;
            maxPurchases = c.maxPurchases;
            urls = c.urls;
        }
    }
}
