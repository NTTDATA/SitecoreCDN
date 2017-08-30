using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Caching;
using Sitecore.Configuration;

namespace NTTData.SitecoreCDN.Caching
{
    /// <summary>
    /// This cache keeps mappings of original urls to dehydrated cdn urls
    /// /path/to/file.ext?a=1&b=2  => http://cdnhostname/path/to/file!cf!a=1!b=2.ext
    /// </summary>
    public class ExcludeIncludeCache : CustomCache
    {

        private TimeSpan _cacheTime;

        public ExcludeIncludeCache(string name, long maxSize)
            : base(name, maxSize)
        {
            this.InnerCache.Scavengable = true;
            _cacheTime = Settings.GetTimeSpanSetting("SitecoreCDN.UrlVersionCacheTime", "00:05:00");
        }


        public bool? GetResult(string path)
        {
            bool? output = null;
            object result = this.GetObject(path);
            if (result != null)
            {
                output = (bool)result;
            }
            return output;
        }

        public void SetResult(string path, bool result)
        {
            this.SetObject(path, result);
            //this.SetString(path, url, DateTime.UtcNow.Add(_cacheTime));
        }


    }
}
