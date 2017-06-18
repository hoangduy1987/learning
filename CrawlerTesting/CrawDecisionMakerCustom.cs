using System;
using System.Linq;
using System.Net;
using Abot.Core;
using Abot.Poco;

namespace CrawlerTesting
{
    public class CrawDecisionMakerCustom : ICrawlDecisionMaker
    {
        public CrawlDecision ShouldCrawlPage(PageToCrawl pageToCrawl, CrawlContext crawlContext)
        {
            throw new NotImplementedException();
        }

        public CrawlDecision ShouldCrawlPageLinks(CrawledPage crawledPage, CrawlContext crawlContext)
        {
            throw new NotImplementedException();
        }

        public CrawlDecision ShouldDownloadPageContent(CrawledPage crawledPage, CrawlContext crawlContext)
        {
            if (crawledPage == null)
                return new CrawlDecision { Allow = false, Reason = "Null crawled page" };

            if (crawlContext == null)
                return new CrawlDecision { Allow = false, Reason = "Null crawl context" };

            if (crawledPage.HttpWebResponse == null)
                return new CrawlDecision { Allow = false, Reason = "Null HttpWebResponse" };

            if (crawledPage.HttpWebResponse.StatusCode != HttpStatusCode.OK)
                return new CrawlDecision { Allow = false, Reason = "HttpStatusCode is not 200" };

            if(!crawledPage.AngleSharpHtmlDocument.All.Any(p => p.InnerHtml.ToLower().Contains("amaris")))
                return new CrawlDecision { Allow = false, Reason = "Not contain Amaris word" };

            return new CrawlDecision { Allow = true };
        }

        public CrawlDecision ShouldRecrawlPage(CrawledPage crawledPage, CrawlContext crawlContext)
        {
            throw new NotImplementedException();
        }
    }
}
