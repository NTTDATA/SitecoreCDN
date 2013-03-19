using Sitecore.Pipelines.HttpRequest;
using System.Web;
using NTTData.SitecoreCDN.Filters;
using NTTData.SitecoreCDN.Configuration;
using Sitecore.Web;
using Sitecore;
using System.Linq;
using Sitecore.Resources.Media;
using NTTData.SitecoreCDN.Providers;
using Sitecore.Diagnostics;
using Sitecore.Configuration;
using NTTData.SitecoreCDN.Handlers;
using System;

namespace NTTData.SitecoreCDN.Pipelines
{


    /// <summary>
    /// HttpRequest Pipeline step to attach the media url replacer filter
    /// </summary>
    public class CDNAttachFilter : HttpRequestProcessor
    {
        public override void Process(HttpRequestArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            if (!CDNSettings.Enabled)
                return;

            bool shouldFilter = (Sitecore.Context.Item != null || CDNManager.ShouldProcessRequest(args.Url.FilePathWithQueryString)) &&   // if an item is resolved (this is a page request) or file ext is listed in <processRequests>
                                !CDNManager.ShouldExcludeProcessRequest(args.Url.FilePathWithQueryString) && // if the url is not on the excluded list
                                Sitecore.Context.Site != null && // and a site was resolved
                                Sitecore.Context.PageMode.IsNormal &&  // and the site is not in editing mode
                                !string.IsNullOrEmpty(CDNManager.GetCDNHostName()); // and the current site is not in the excluded sites list

            // querystring cdn=1  to force replacement
            // querystring cdn=0  to force no replacement
            Tristate force = MainUtil.GetTristate(WebUtil.GetQueryString("cdn"), Tristate.Undefined);
            if (force == Tristate.False)
                shouldFilter = false;
            else if (force == Tristate.True)
                shouldFilter = true;

            if (shouldFilter)
            {
                var response = HttpContext.Current.Response;
                if (response != null)
                {
                    // replace the default response filter stream with our replacer filter
                    response.Filter = new MediaUrlFilter(response.Filter);
                }
            }





        }
    }
}