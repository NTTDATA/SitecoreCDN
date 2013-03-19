using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Sitecore.Web;
using Sitecore.SecurityModel.Cryptography;
using Sitecore.IO;
using Sitecore;
using Sitecore.Diagnostics;
using System.Text.RegularExpressions;
using NTTData.SitecoreCDN.Minifiers;
using Sitecore.Text;
using NTTData.SitecoreCDN.Configuration;

namespace NTTData.SitecoreCDN.Handlers
{
    /// <summary>
    /// This handler will minify .js and .css file requests.
    /// </summary>
    public class MinifyHandler : IHttpHandler
    {
        public bool IsReusable
        {
            get { return true; }
        }
        private bool _success = false;
        public virtual bool Success
        {
            get { return _success; }
        }

        public void ProcessRequest(HttpContext context)
        {
            // set from an earlier in pipeline by CDNInterceptPipeline 
            string minifyPath = StringUtil.GetString(context.Items["MinifyPath"]);

            if (string.IsNullOrEmpty(minifyPath))
            {
                context.Response.StatusCode = 404;
                context.Response.End();
            }
            
            
            UrlString url = new UrlString(minifyPath);
            string localPath = url.Path;

            string filePath = FileUtil.MapPath(localPath);

            // if the request is a .js file
            if (localPath.EndsWith(".js"))
            {
                HashEncryption hasher = new HashEncryption(HashEncryption.EncryptionProvider.MD5);
                if (!string.IsNullOrEmpty(localPath))
                {
                    // generate a unique filename for the cached .js version
                    string cachedFilePath = FileUtil.MapPath(string.Format("/App_Data/MediaCache/{0}.js", hasher.Hash(url.ToString())));

                    // if it doesn't exist create it
                    if (!FileUtil.FileExists(cachedFilePath))
                    {
                        // if the original file exsits minify it
                        if (FileUtil.FileExists(filePath))
                        {
                            JsMinifier minifier = new JsMinifier();
                            string minified = minifier.Minify(filePath);
                            FileUtil.WriteToFile(cachedFilePath, minified);
                        }
                    }

                    if (FileUtil.FileExists(cachedFilePath))
                    {
                        context.Response.ClearHeaders();
                        context.Response.Cache.SetExpires(DateTime.Now.AddDays(14));
                        context.Response.AddHeader("Content-Type", "application/x-javascript; charset=utf-8");
                        context.Response.WriteFile(cachedFilePath);
                        context.Response.End();
                        _success = true;
                    }
                }
            }
            // if the request is a .css file
            else if (localPath.EndsWith(".css"))
            {
                HashEncryption hasher = new HashEncryption(HashEncryption.EncryptionProvider.MD5);
                if (!string.IsNullOrEmpty(localPath))
                {
                    // generate a unique filename for the cached .css version
                    string cachedFilePath = FileUtil.MapPath(string.Format("/App_Data/MediaCache/{0}.css", hasher.Hash(url.ToString())));

                    // if it doesn't exist create it
                    if (!FileUtil.FileExists(cachedFilePath))
                    {
                        // if the original file exsits minify it
                        if (FileUtil.FileExists(filePath))
                        {
                            CssMinifier minifier = new CssMinifier();
                            string minified = CssMinifier.Minify(FileUtil.ReadFromFile(filePath));

                            // if Css Processing is enabled, replace any urls inside the css file.
                            if (CDNSettings.ProcessCss)
                            {
                                // find all css occurences of url([url])
                                Regex reReplaceUrl = new Regex("url\\(\\s*['\"]?([^\"')]+)['\"]?\\s*\\)");

                                try
                                {
                                    // replacing  url([url]) with url([cdnUrl]) in css
                                    minified = reReplaceUrl.Replace(minified, (m) =>
                                    {
                                        string oldUrl = "";
                                        if (m.Groups.Count > 1)
                                            oldUrl = m.Groups[1].Value;

                                        if (WebUtil.IsInternalUrl(oldUrl))
                                        {
                                            if (oldUrl.StartsWith("."))
                                            {
                                                oldUrl = VirtualPathUtility.Combine(url.Path, oldUrl);
                                            }
                                            string newUrl = CDNManager.ReplaceMediaUrl(oldUrl, string.Empty);
                                            if (!string.IsNullOrEmpty(newUrl))
                                            {
                                                return m.Value.Replace(m.Groups[1].Value, newUrl);
                                            }
                                        }

                                        return m.Value;
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Log.Error("Minify error", ex, this);
                                }
                            }

                            FileUtil.WriteToFile(cachedFilePath, minified);
                        }
                    }

                    if (FileUtil.FileExists(cachedFilePath))
                    {
                        context.Response.ClearHeaders();
                        context.Response.Cache.SetExpires(DateTime.Now.AddDays(14));
                        context.Response.AddHeader("Content-Type", "text/css; charset=utf-8");
                        context.Response.TransmitFile(cachedFilePath);
                        context.Response.End();
                        _success = true;
                    }
                }
            }







        }
    }
}
