using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Web;
using Sitecore.Text;
using Sitecore;
using NTTData.SitecoreCDN.Configuration;
using Sitecore.Configuration;
using Sitecore.Resources.Media;
using Sitecore.Data.Items;
using System.Reflection;
using Sitecore.Reflection;
using Sitecore.IO;
using HtmlAgilityPack;
using NTTData.SitecoreCDN.Caching;
using Sitecore.Sites;
using System.Collections.Specialized;
using Sitecore.Security.Domains;
using System.Xml.Linq;
using Sitecore.Diagnostics;

namespace NTTData.SitecoreCDN.Providers
{
    /// <summary>
    /// Contains all CDN related provider methods.
    /// </summary>
    public class CDNProvider
    {
        private UrlCache _cache; // cache url/security/tracking results here
        private ExcludeIncludeCache _excludeUrlCache; // cache url excludes here
        private ExcludeIncludeCache _includeUrlCache; // cache url includes here
        private ExcludeIncludeCache _excludeRequestCache; // cache url request excludes here


        /// <summary>
        /// The token used to stop url replacement
        /// </summary>
        public virtual string StopToken
        {
            get { return "ncdn"; }
        }

        /// <summary>
        /// special value to indicate no caching
        /// </summary>
        public virtual string NoCacheToken
        {
            get { return "#nocache#"; }
        }

        public CDNProvider()
        {
            long cacheSize = StringUtil.ParseSizeString(Settings.GetSetting("SitecoreCDN.FileVersionCacheSize", "5MB"));
            _cache = new UrlCache("CDNUrl", cacheSize);
            _excludeUrlCache = new ExcludeIncludeCache("CDNExcludes", cacheSize);
            _includeUrlCache = new ExcludeIncludeCache("CDNIncludes", cacheSize);
            _excludeRequestCache = new ExcludeIncludeCache("CDNRequestExcludes", cacheSize);
        }

        /// <summary>
        /// replace appropriate media urls in a full HtmlDocument
        /// </summary>
        /// <param name="doc"></param>
        public virtual void ReplaceMediaUrls(HtmlDocument doc)
        {
            try
            {
                string cdnHostname = GetCDNHostName();
                // for any <link href=".." /> do replacement
                var links = doc.DocumentNode.SelectNodes("//link");
                if (links != null)
                {
                    foreach (HtmlNode link in links)
                    {
                        string href = link.GetAttributeValue("href", "");
                        if (!string.IsNullOrEmpty(href) && !UrlIsExluded(href))  // don't replace VisitorIdentification.aspx
                        {
                            link.SetAttributeValue("href", ReplaceMediaUrl(href, cdnHostname));
                        }
                    }
                }


                HtmlNode scriptTargetNode = null;

                if (CDNSettings.FastLoadJsEnabled)
                {
                    // if <div id='cdn_scripts'></div> exists, append scripts here rather than </body>
                    scriptTargetNode = doc.DocumentNode.SelectSingleNode("//*[@id='cdn_scripts']");
                    if (scriptTargetNode == null)
                    {
                        scriptTargetNode = doc.DocumentNode.SelectSingleNode("//body");
                    }
                }


                var imgscripts = doc.DocumentNode.SelectNodes("//img | //script");
                // for any <img src="..." /> or <script src="..." /> do replacements
                if (imgscripts != null)
                {
                    foreach (HtmlNode element in imgscripts)
                    {
                        string src = element.GetAttributeValue("src", "");
                        if (!string.IsNullOrEmpty(src) && !UrlIsExluded(src))
                        {
                            element.SetAttributeValue("src", ReplaceMediaUrl(src, cdnHostname));
                        }

                        // move scripts to scriptTargetNode if FastLoadJsEnabled = true
                        if (element.Name == "script" && scriptTargetNode != null)
                        {
                            scriptTargetNode.AppendChild(element.Clone());
                            element.Remove();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("ReplaceMediaUrls", ex, this);
            }

        }


        /// <summary>
        /// Rewrites media urls to point to CDN hostname and dehydrates querystring into filename
        /// </summary>
        /// <param name="inputUrl">/path/to/file.ext?a=1&b=2</param>
        /// <returns>http://cdnHostname/path/to/file!cf!a=1!b=2.ext</returns>
        public virtual string ReplaceMediaUrl(string inputUrl, string cdnHostname)
        {
            //string versionKey = inputUrl + "_v";
            //string updatedKey = inputUrl + "_d";
            string cachedKey = string.Concat(WebUtil.GetScheme(), inputUrl);
            try
            {

                string cachedUrl = _cache.GetUrl(cachedKey);

                if (!string.IsNullOrEmpty(cachedUrl))
                {
                    return cachedUrl;
                }

                // ignore fully qualified urls or data:
                if (WebUtil.IsExternalUrl(inputUrl) || inputUrl.StartsWith("data:") || inputUrl.StartsWith("//"))
                    return inputUrl;

                UrlString url = new UrlString(WebUtil.NormalizeUrl(inputUrl));
                UrlString originalUrl = new UrlString(WebUtil.NormalizeUrl(inputUrl));

                //  if the stoptoken ex. ?nfc=1  is non-empty, don't replace this url
                if (!string.IsNullOrEmpty(url[this.StopToken]))
                {
                    url.Remove(this.StopToken);
                }
                else
                {

                    if (!string.IsNullOrEmpty(cdnHostname))
                        url.HostName = cdnHostname;  // insert CDN hostname

                    if (CDNSettings.MatchProtocol)
                        url.Protocol = WebUtil.GetScheme();

                    url.Path = StringUtil.EnsurePrefix('/', url.Path);  //ensure first "/" before ~/media


                    if (CDNSettings.FilenameVersioningEnabled)
                    {
                        // if this is a media library request
                        if (inputUrl.Contains(Settings.Media.MediaLinkPrefix))
                        {
                            string version = url["vs"] ?? string.Empty;
                            string updated = string.Empty;


                            // get sitecore path of media item
                            string mediaItemPath = GetMediaItemPath(url.Path);
                            if (!string.IsNullOrEmpty(mediaItemPath) && Sitecore.Context.Database != null)
                            {
                                Item mediaItem = null;
                                if (!string.IsNullOrEmpty(version))
                                {
                                    mediaItem = Sitecore.Context.Database.GetItem(mediaItemPath, Sitecore.Context.Language, Sitecore.Data.Version.Parse(version));
                                }
                                else
                                {
                                    mediaItem = Sitecore.Context.Database.SelectSingleItem(mediaItemPath);
                                }

                                if (mediaItem == null)
                                {
                                    // no change to url
                                    url = originalUrl;
                                }
                                else
                                {
                                    // do not replace url if media item isn't public or requires Analytics processing
                                    // keep local url for this case
                                    if (!this.IsMediaPubliclyAccessible(mediaItem) || IsMediaAnalyticsTracked(mediaItem))
                                    {
                                        // no change to url
                                        url = originalUrl;
                                    }
                                    else
                                    {
                                        version = mediaItem.Version.Number.ToString();
                                        updated = DateUtil.ToIsoDate(mediaItem.Statistics.Updated);
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(version))
                            {
                                // append version number qs
                                url.Add("vs", version);
                            }
                            if (!string.IsNullOrEmpty(updated))
                            {
                                // append  timestamp qs
                                url.Add("d", updated);
                            }
                        }
                        else // else this is a static file url
                        {
                            string updated = string.Empty;

                            if (string.IsNullOrEmpty(updated))
                            {
                                if (FileUtil.FileExists(url.Path))
                                {
                                    DateTime lastWrite = FileUtil.GetFileWriteTime(url.Path);
                                    updated = DateUtil.ToIsoDate(lastWrite);
                                }
                            }
                            if (!string.IsNullOrEmpty(updated))
                            {
                                // append timestamp qs
                                url.Add("d", updated);
                            }

                            if (CDNSettings.MinifyEnabled && (url.Path.EndsWith(".css") || url.Path.EndsWith(".js")))
                                url.Add("min", "1");
                        }
                    }
                }

                string outputUrl = url.ToString().TrimEnd('?');//prevent trailing ? with blank querystring

                _cache.SetUrl(cachedKey, outputUrl);

                return outputUrl;
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("ReplaceMediaUrl {0} {1}", cdnHostname, inputUrl), ex, this);
                return inputUrl;
            }


        }





        /// <summary>
        /// Tells you if the url is excluded by ExcludeUrlPatterns in .config
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public virtual bool UrlIsExluded(string url)
        {
            bool? exc = _excludeUrlCache.GetResult(url);
            if (exc.HasValue)
                return exc.Value;
            bool output = CDNSettings.ExcludeUrlPatterns.Any(re => re.IsMatch(url));
            _excludeUrlCache.SetResult(url, output);
            return output;
        }

        /// <summary>
        /// Tells you if an incoming request's url should have it's contents Url replaced.
        /// ProcessRequestPatterns in .config
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public virtual bool ShouldProcessRequest(string url)
        {
            bool? inc = _includeUrlCache.GetResult(url);
            if (inc.HasValue)
                return inc.Value;
            bool output = CDNSettings.ProcessRequestPatterns.Any(re => re.IsMatch(url));
            _includeUrlCache.SetResult(url, output);
            return output;
        }

        /// <summary>
        /// Tells you if an incoming request's url should NOT hav its contents Url replaced.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public virtual bool ShouldExcludeRequest(string url)
        {
            bool? exc = _excludeRequestCache.GetResult(url);
            if (exc.HasValue)
                return exc.Value;
            bool output = CDNSettings.ExcludeRequestPatterns.Any(re => re.IsMatch(url));
            _excludeRequestCache.SetResult(url, output);
            return output;
        }


        /// <summary>
        /// Extracts the sitecore media item path from a Url 
        /// </summary>
        /// <param name="localPath">~/media/path/to/file.ashx?w=1</param>
        /// <returns>/sitecore/media library/path/to/file</returns>
        public virtual string GetMediaItemPath(string localPath)
        {
            MediaRequest mr = new MediaRequest();
            // this is a hack to access a private method in MediaRequest
            MethodInfo mi = ReflectionUtil.GetMethod(mr, "GetMediaPath", true, true, new object[] { localPath });
            if (mi != null)
            {
                return (string)ReflectionUtil.InvokeMethod(mi, new object[] { localPath }, mr);
            }
            return null;
        }


        /// <summary>
        /// Attempts to retrieve the CDN hostname for the current site
        /// </summary>
        /// <returns></returns>
        public virtual string GetCDNHostName()
        {
            return GetCDNHostName(Sitecore.Context.Site);
        }

        /// <summary>
        /// Attempts to retrive the CDN hostname for this site
        /// </summary>
        /// <param name="siteContext"></param>
        /// <returns></returns>
        public virtual string GetCDNHostName(SiteContext siteContext)
        {
            if (siteContext == null)
                return string.Empty;
            // try to find <site name='[sitename]'  cdnHostName='[cdnhostname]' />
            return StringUtil.GetString(siteContext.Properties.Get("cdnHostName"));
        }



        /// <summary>
        /// Is this media item publicly accessible by the anonymous user?
        /// </summary>
        /// <param name="media"></param>
        /// <returns></returns>
        public virtual bool IsMediaPubliclyAccessible(MediaItem media)
        {
            string cacheKey = media.ID.ToString() + "_public";
            string cached = _cache.GetUrl(cacheKey);
            bool output = true;

            // cached result
            if (!string.IsNullOrEmpty(cached))
            {
                output = MainUtil.GetBool(cached, true);
            }
            else
            {
                Domain domain = Sitecore.Context.Domain ?? Factory.GetDomain("extranet");
                var anon = domain.GetAnonymousUser();
                if (anon != null)
                    output = media.InnerItem.Security.CanRead(anon);

                _cache.SetUrl(cacheKey, output.ToString());
            }
            return output;
        }

        /// <summary>
        /// Is this media item Tracked by DMS?
        /// </summary>
        /// <param name="media"></param>
        /// <returns></returns>
        public virtual bool IsMediaAnalyticsTracked(MediaItem media)
        {
            try
            {
                if (!Settings.Analytics.Enabled)
                    return false;

                string cacheKey = media.ID.ToString() + "_tracked";
                string cached = _cache.GetUrl(cacheKey);
                bool output = false;

                // cached result
                if (!string.IsNullOrEmpty(cached))
                {
                    output = MainUtil.GetBool(cached, true);
                }
                else
                {
                    string aData = media.InnerItem["__Tracking"];

                    if (string.IsNullOrEmpty(aData))
                    {
                        output = false;
                    }
                    else
                    {
                        XElement el = XElement.Parse(aData);
                        var ignore = el.Attribute("ignore");

                        if (ignore != null && ignore.Value == "1")
                        {
                            output = false;
                        }
                        else
                        {
                            // if the tracking element has any events, campaigns or profiles.
                            output = el.Elements("event").Any() || el.Elements("campaign").Any() || el.Elements("profile").Any();
                        }
                    }
                    _cache.SetUrl(cacheKey, output.ToString());
                }
                return output;
            }
            catch (Exception ex)
            {
                Log.Error("IsMediaAnalyticsTracked", ex, this);
                return false;
            }
        }
    }
}
