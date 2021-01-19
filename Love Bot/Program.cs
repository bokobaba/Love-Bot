using CommandLine;
using Love_Bot.Sites;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Love_Bot {
    class Program {
        private static Dictionary<string, WebsiteConfig> configs = new Dictionary<string, WebsiteConfig>();
        private static Dictionary<string, Dictionary<string, string>> payment = new Dictionary<string, Dictionary<string, string>>();
        private static Dictionary<string, Tuple<Thread, Website>> threads = new Dictionary<string, Tuple<Thread, Website>>();
        private static int checkInterval = 30;

        private class Options {
            [Value(0)]
            public string configPath { get; set; }
            [Value(1)]
            public string paymentPath { get; set; }
            [Option('t')]
            public bool template { get; set; }
        }

        private static void Main(string[] args) {
            if (args.Length < 2 || args.Length > 3)
                Console.WriteLine("usage: [-t print_template] LoveBot path/to/config.json path/to/payment.json");

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o => {
                    if (o.template) {
                        CreateTemplate(o.configPath, o.paymentPath);
                        return;
                    }
                    configs = LoadConfig(o.configPath);
                    payment = LoadPaymentInfo(o.paymentPath);
                }).WithNotParsed<Options>(e => {
                    Console.WriteLine("usage: [-t print_template] LoveBot path/to/config.json path/to/payment.json");
                });
            if (configs is null || payment is null) return;
            if (configs.Count == 0 || payment.Count == 0) return;

            Console.WriteLine("success");

            foreach (KeyValuePair<string, WebsiteConfig> config in configs) {
                Website site = GetWebsite(config.Key, config.Value);
                if (site is null)
                    continue;
                CreateBotThread(config.Key, site);
            }

            new Thread(() => CheckConfigs(args[0])).Start();
        }

        private static void CreateBotThread(string name, Website site) {
            Console.WriteLine("creating bot: " + name);
            site.abort = new CancellationTokenSource();
            Thread thread = new Thread(async () => await site.Run());
            thread.Name = name;
            threads.Add(name, new Tuple<Thread, Website>(thread, site));
            thread.Start();
        }

        private static void CheckConfigs(string path) {
            while (true) {
                Thread.Sleep(checkInterval * 1000);

                foreach(KeyValuePair<string, Tuple<Thread, Website>> kvp in threads) {
                    if (kvp.Value.Item2.searchTask.IsCompleted) {
                        Console.WriteLine("removing thread: " + kvp.Value.Item1.Name);
                        kvp.Value.Item2.abort.Cancel();
                        threads.Remove(kvp.Key);
                    }
                }

                Console.WriteLine("checking config file");

                if (!File.Exists(path)) continue;

                Dictionary<string, WebsiteConfig> newConfigs = LoadConfig(path);

                if (newConfigs is null || newConfigs.Count == 0) {
                    continue;
                }

                foreach (string name in configs.Keys) {
                    if (threads.ContainsKey(name)) {
                        Tuple<Thread, Website> t = threads[name];
                        if (!newConfigs.Keys.Contains(name)) {
                            Console.WriteLine("removing bot: " + name);
                            t.Item2.abort.Cancel();
                            threads.Remove(name);
                        }
                    }
                }

                configs = newConfigs;

                foreach (KeyValuePair<string, WebsiteConfig> config in configs) {
                    if (threads.ContainsKey(config.Key)) {
                        Console.WriteLine("updating bot: " + config.Key);
                        Website w = threads[config.Key].Item2;
                        w.UpdateConfigs(config.Value);
                        Console.WriteLine("done");
                    }
                    else {
                        Website site = GetWebsite(config.Key, config.Value);
                        if (site is null)
                            continue;
                        CreateBotThread(config.Key, site);
                    }
                }
            }
        }

        private static Website GetWebsite(string name, WebsiteConfig config) {
            if (config.urls.Length < 1)
                return null;
            string site;
            try {
                site = new Uri(config.urls[0]).Host;
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return null;
            }
            for (int i = 0; i < config.urls.Length; ++i) {
                if (!site.Equals(new Uri(config.urls[i]).Host)) {
                    Console.WriteLine(name + ": all urls must use same domain");
                    return null;
                }
            }
            if (site.ToLower().Equals("www.walmart.com")) {
                return new Walmart(name, config, payment);
            }
            else if (site.ToLower().Equals("www.amazon.com")) {
                return new Amazon(name, config, payment);
            }
            else if (site.ToLower().Equals("www.gamestop.com")) {
                return new Gamestop(name, config, payment);
            }
            else if (site.ToLower().Equals("www.bestbuy.com")) {
                return new BestBuy(name, config, payment);
            }
            else {
                Console.WriteLine(site + " is not supported");
                return null;
            }
        }

        private static Dictionary<string, WebsiteConfig> LoadConfig(string path) {
            Dictionary<string, WebsiteConfig> configs;
            try {
                configs = JsonConvert.DeserializeObject<Dictionary<string, WebsiteConfig>>(File.ReadAllText(path));
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return null;
            }
            //Console.WriteLine("length = " + configs.Count);
            //foreach (KeyValuePair<string, WebsiteConfig> c in configs) {
            //    if (c.Value.login.Length < 2) {
            //        Console.WriteLine("invalid number of login parameters for " + c.Key);
            //        configs.Remove(c.Key);
            //    }

            //    Console.WriteLine(c.Key + "\n" + c.Value);
            //}
            File.Delete(path);
            return configs;
        }

        private static Dictionary<string, Dictionary<string, string>> LoadPaymentInfo(string path) {
            Dictionary<string, Dictionary<string, string>> payment;
            try {
                payment = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(path));
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return null;
            }
            //foreach (Dictionary<string, string> d in payment.Values) {
            //    d.Select(i => $"{i.Key}: {i.Value}").ToList().ForEach(Console.WriteLine);
            //}

            //payment["billingInfo"].Select(i => $"{i.Key}: {i.Value}").ToList().ForEach(Console.WriteLine);
            File.Delete(path);
            return payment;
        }

        private static void CreateTemplate(string configPath, string paymentPath) {
            Console.WriteLine("generating templates for " + configPath + " and " + paymentPath);
            try {
                File.WriteAllText(configPath, JsonConvert.SerializeObject(
                    new Dictionary<string, WebsiteConfig>() {
                    { "botName", new WebsiteConfig() },
                    { "botName2", new WebsiteConfig() }}, Formatting.Indented));
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return;
            }

            try {
                File.WriteAllText(paymentPath, JsonConvert.SerializeObject(
                    new Dictionary<string, Dictionary<string, string>>() {
                    {
                        "billingInfo",
                        new Dictionary<string, string>() {
                            { "address", "123 Fake Street" },
                            { "address2", "Line 2" },
                            { "firstName", "firstName" },
                            { "lastName", "lastName" },
                            { "state", "TX" },
                            { "city", "city" },
                            { "zipcode", "12345" }
                        }
                    },
                    {
                        "shippingInfo",
                        new Dictionary<string, string>() {
                            { "address", "123 Fake Street" },
                            { "address2", "Line 2" },
                            { "firstName", "firstName" },
                            { "lastName", "lastName" },
                            { "state", "TX" },
                            { "city", "city" },
                            { "zipcode", "12345" }
                        }
                    },
                    {
                        "paymentInfo",
                        new Dictionary<string, string>() {
                            { "creditCardNum", "1234567890123456" },
                            { "cvv", "123" },
                            { "expMonth", "01" },
                            { "expYear", "2000"
                        }
                    }

                }}, Formatting.Indented));
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return;
            }
        }
    }
}
