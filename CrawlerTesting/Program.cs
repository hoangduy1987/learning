using System;
using System.Linq;
using System.Net;
using Abot.Crawler;
using Abot.Poco;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using HtmlAgilityPack;

namespace CrawlerTesting
{
    class Program
    {
        static void Main(string[] args)
        {
            //log4net.Config.XmlConfigurator.Configure();

            args = new[] { "https://www.indeed.fr/cmp/Amaris/reviews?fcountry=ALL&sort=any" };
            Uri uriToCrawl = GetSiteToCrawl(args);

            IWebCrawler crawler = GetCustomBehaviorUsingLambdaWebCrawler();

            //Subscribe to any of these asynchronous events, there are also sychronous versions of each.
            //This is where you process data about specific events of the crawl
            crawler.PageCrawlStartingAsync += crawler_ProcessPageCrawlStarting;
            crawler.PageCrawlCompletedAsync += crawler_ProcessPageCrawlCompleted;
            crawler.PageCrawlDisallowedAsync += crawler_PageCrawlDisallowed;
            crawler.PageLinksCrawlDisallowedAsync += crawler_PageLinksCrawlDisallowed;

            //Start the crawl
            //This is a synchronous call
            var result = crawler.Crawl(uriToCrawl);

            if (result.ErrorOccurred)
                Console.WriteLine("Crawl of {0} completed with error: {1}", result.RootUri.AbsoluteUri, result.ErrorException.Message);
            else
                Console.WriteLine("Crawl of {0} completed without error.", result.RootUri.AbsoluteUri);
            Console.ReadLine();
        }

        #region [ Private ]

        private static Uri GetSiteToCrawl(string[] args)
        {
            string userInput;
            if (args.Length < 1)
            {
                Console.WriteLine("Please enter ABSOLUTE url to crawl:");
                userInput = Console.ReadLine();
            }
            else
            {
                userInput = args[0];
            }

            if (string.IsNullOrWhiteSpace(userInput))
                throw new ApplicationException("Site url to crawl is as a required parameter");

            return new Uri(userInput);
        }

        private static IWebCrawler GetCustomBehaviorUsingLambdaWebCrawler()
        {
            IWebCrawler crawler = GetCrawlerConfig();

            //Register a lambda expression that will make Abot not crawl any url that has the word "ghost" in it.
            //For example http://a.com/ghost, would not get crawled if the link were found during the crawl.
            //If you set the log4net log level to "DEBUG" you will see a log message when any page is not allowed to be crawled.
            //NOTE: This is lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPage method is run.
            crawler.ShouldCrawlPage((pageToCrawl, crawlContext) =>
            {
                //if (pageToCrawl.Uri.AbsoluteUri.Contains("ghost"))
                //    return new CrawlDecision { Allow = false, Reason = "Scared of ghosts" };

                if(pageToCrawl.CrawlDepth > 3)
                    return new CrawlDecision {Allow = false,Reason = "Should not craw more than level 3"};

                return new CrawlDecision { Allow = true };
            });

            //Register a lambda expression that will tell Abot to not download the page content for any page after 5th.
            //Abot will still make the http request but will not read the raw content from the stream
            //NOTE: This lambda is run AFTER the regular ICrawlDecsionMaker.ShouldDownloadPageContent method is run
            crawler.ShouldDownloadPageContent((crawledPage, crawlContext) =>
            {
                if (crawlContext.CrawledCount >= 5)
                    return new CrawlDecision { Allow = false, Reason = "We already downloaded the raw page content for 5 pages" };

                var isDownloadable = true;

                var body = crawledPage.AngleSharpHtmlDocument.Body;

                if (body.HasChildNodes)
                {
                    foreach (var nodeChildNode in body.ChildNodes)
                    {
                        isDownloadable = CheckAmarisContent(nodeChildNode);
                    }
                }

                if(!isDownloadable)
                    return new CrawlDecision { Allow = false, Reason = "No contain amaris" };

                return new CrawlDecision { Allow = true };
            });

            //Register a lambda expression that will tell Abot to not crawl links on any page that is not internal to the root uri.
            //NOTE: This lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPageLinks method is run
            crawler.ShouldCrawlPageLinks((crawledPage, crawlContext) =>
            {
                if (!crawledPage.IsInternal)
                    return new CrawlDecision { Allow = false, Reason = "We dont crawl links of external pages" };

                return new CrawlDecision { Allow = true };
            });

            return crawler;
        }

        private static IWebCrawler GetCrawlerConfig()
        {
            //Create a config object manually
            var config = new CrawlConfiguration
            {
                CrawlTimeoutSeconds = 0,
                DownloadableContentTypes = "text/html, text/plain",
                IsExternalPageCrawlingEnabled = false,
                IsExternalPageLinksCrawlingEnabled = false,
                IsRespectRobotsDotTextEnabled = true,
                IsUriRecrawlingEnabled = false,
                MaxConcurrentThreads = 10,
                MaxPagesToCrawl = 10,
                MaxPagesToCrawlPerDomain = 0,
                MinCrawlDelayPerDomainMilliSeconds = 1000
            };

            //Add you own values without modifying Abot's source code.
            //These are accessible in CrawlContext.CrawlConfuration.ConfigurationException object throughout the crawl
            config.ConfigurationExtensions.Add("Somekey1", "SomeValue1");
            config.ConfigurationExtensions.Add("Somekey2", "SomeValue2");

            //Initialize the crawler with custom configuration created above.
            //This override the app.config file values
            return new PoliteWebCrawler(config, null, null, null, null, null, null, null, null);
        }

        #endregion

        #region [ Events ]

        private static void crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
            //Process data
        }

        private static void crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            var crawledPage = e.CrawledPage;

            if (crawledPage.WebException != null || crawledPage.HttpWebResponse.StatusCode != HttpStatusCode.OK)
                Console.WriteLine("Crawl of page failed {0}", crawledPage.Uri.AbsoluteUri);
            else
                Console.WriteLine("Crawl of page succeeded {0}", crawledPage.Uri.AbsoluteUri);

            if (string.IsNullOrEmpty(crawledPage.Content.Text))
                Console.WriteLine("Page had no content {0}", crawledPage.Uri.AbsoluteUri);
            else
            {
                System.IO.File.WriteAllText(@"E:\Learning\TestText_" + DateTime.Now.Millisecond + ".txt", crawledPage.Content.Text);
            }

            //foreach (var node in crawledPage.HtmlDocument.DocumentNode.SelectNodes("//*"))
            //{
            //    if (node.InnerHtml.ToLower().Contains("amaris"))
            //        System.IO.File.WriteAllText(@"E:\Learning\TestText_" + DateTime.Now.Millisecond + ".txt", node.InnerHtml);
            //}
        }

        private static void crawler_PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
            //Process data
        }

        private static void crawler_PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
            //Process data
        }

        private static bool CheckAmarisContent(INode node)
        {
            if (node.HasChildNodes)
            {
                foreach (var nodeChild in node.ChildNodes)
                {
                    CheckAmarisContent(nodeChild);
                }
            }

            if (node.NodeValue.ToLower().Contains("amaris"))
                return true;
            return false;
        }

        #endregion
    }
}
