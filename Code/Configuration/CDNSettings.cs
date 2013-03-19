using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sitecore.Configuration;
using Sitecore.Xml;
using System.Xml;
using Sitecore;
using Sitecore.Sites;

namespace NTTData.SitecoreCDN.Configuration
{

    public static partial class CDNSettings
    {
       
            private static Regex[] _excludeUrls;
            private static Regex[] _processRequests;
            private static Regex[] _excludeRequests;

            private static readonly object _lock = new object();

            private static bool? _enabled;
            private static bool? _filenameVersioningEnabled;
            private static bool? _minifyEnabled;
            private static bool? _processCSS;
            private static bool? _fastLoadJsEnabled;
            private static bool? _debugParser;
            private static bool? _matchProtocol;
        


            static CDNSettings()
            {

            }

            public static Regex[] ExcludeUrlPatterns
            {
                get
                {
                    if (_excludeUrls == null)
                    {
                        lock (_lock)
                        {
                            if (_excludeUrls == null)
                            {
                                List<Regex> regexes = new List<Regex>();
                                foreach (XmlNode regexNode in Factory.GetConfigNodes("cdn/excludeUrls/regex"))
                                {
                                    string pattern = XmlUtil.GetAttribute("pattern", regexNode);
                                    if (!string.IsNullOrEmpty(pattern))
                                    {
                                        regexes.Add(new Regex(pattern, RegexOptions.IgnoreCase));
                                    }
                                }
                                _excludeUrls = regexes.ToArray();
                            }
                        }
                    }
                    return _excludeUrls;
                }
            }


            public static Regex[] ProcessRequestPatterns
            {
                get
                {
                    if (_processRequests == null)
                    {
                        lock (_lock)
                        {
                            if (_processRequests == null)
                            {
                                List<Regex> regexes = new List<Regex>();
                                foreach (XmlNode regexNode in Factory.GetConfigNodes("cdn/processRequests/regex"))
                                {
                                    string pattern = XmlUtil.GetAttribute("pattern", regexNode);
                                    if (!string.IsNullOrEmpty(pattern))
                                    {
                                        regexes.Add(new Regex(pattern, RegexOptions.IgnoreCase));
                                    }
                                }
                                _processRequests = regexes.ToArray();
                            }
                        }
                    }
                    return _processRequests;
                }
            }

            public static Regex[] ExcludeRequestPatterns
            {
                get
                {
                    if (_excludeRequests == null)
                    {
                        lock (_lock)
                        {
                            if (_excludeRequests == null)
                            {
                                List<Regex> regexes = new List<Regex>();
                                foreach (XmlNode regexNode in Factory.GetConfigNodes("cdn/excludeRequests/regex"))
                                {
                                    string pattern = XmlUtil.GetAttribute("pattern", regexNode);
                                    if (!string.IsNullOrEmpty(pattern))
                                    {
                                        regexes.Add(new Regex(pattern, RegexOptions.IgnoreCase));
                                    }
                                }
                                _excludeRequests = regexes.ToArray();
                            }
                        }
                    }
                    return _excludeRequests;
                }
            }

            

            public static bool Enabled
            {
                get
                {
                    if (!_enabled.HasValue)
                    {
                        lock (_lock)
                        {
                            if (!_enabled.HasValue)
                            {
                                XmlNode cdnNode = Factory.GetConfigNode("cdn");
                                _enabled = MainUtil.GetBool(XmlUtil.GetAttribute("enabled", cdnNode), false);
                            }
                            
                        }
                    }
                    return _enabled.GetValueOrDefault(false);
                }
            }

            public static bool MinifyEnabled
            {
                get
                {
                    if (!_minifyEnabled.HasValue)
                    {
                        lock (_lock)
                        {
                            if (!_minifyEnabled.HasValue)
                            {
                                XmlNode cdnNode = Factory.GetConfigNode("cdn");
                                _minifyEnabled = MainUtil.GetBool(XmlUtil.GetAttribute("minifyEnabled", cdnNode), false);
                            }
                            
                        }
                    }
                    return _minifyEnabled.GetValueOrDefault(false);
                }
            }

            public static bool ProcessCss
            {
                get
                {
                    if (!_processCSS.HasValue)
                    {
                        lock (_lock)
                        {
                            if (!_processCSS.HasValue)
                            {
                                XmlNode cdnNode = Factory.GetConfigNode("cdn");
                                _processCSS = MainUtil.GetBool(XmlUtil.GetAttribute("processCss", cdnNode), false);
                            }
                            
                        }
                    }
                    return _processCSS.GetValueOrDefault(false);
                }
            }

            public static bool FastLoadJsEnabled
            {
                get
                {
                    if (!_fastLoadJsEnabled.HasValue)
                    {
                        lock (_lock)
                        {
                            if (!_fastLoadJsEnabled.HasValue)
                            {
                                XmlNode cdnNode = Factory.GetConfigNode("cdn");
                                _fastLoadJsEnabled = MainUtil.GetBool(XmlUtil.GetAttribute("fastLoadJsEnabled", cdnNode), false);
                            }
                            
                        }
                    }
                    return _fastLoadJsEnabled.GetValueOrDefault(false);
                }
            }

            public static bool FilenameVersioningEnabled
            {
                get
                {
                    if (!_filenameVersioningEnabled.HasValue)
                    {
                        lock (_lock)
                        {
                            if (!_filenameVersioningEnabled.HasValue)
                            {
                                XmlNode cdnNode = Factory.GetConfigNode("cdn");
                                _filenameVersioningEnabled = MainUtil.GetBool(XmlUtil.GetAttribute("filenameVersioningEnabled", cdnNode), false);
                            }
                            
                        }
                    }
                    return _filenameVersioningEnabled.GetValueOrDefault(false);
                }
            }

            public static bool DebugParser
            {
                get
                {
                    if (!_debugParser.HasValue)
                    {
                        lock (_lock)
                        {
                            if (!_debugParser.HasValue)
                            {
                                XmlNode cdnNode = Factory.GetConfigNode("cdn");
                                _debugParser = MainUtil.GetBool(XmlUtil.GetAttribute("debugParser", cdnNode), false);
                            }
                        }
                    }
                    return _debugParser.GetValueOrDefault(false);
                }
            }

            public static bool MatchProtocol
            {
                get
                {
                    if (!_matchProtocol.HasValue)
                    {
                        lock (_lock)
                        {
                            if (!_matchProtocol.HasValue)
                            {
                                XmlNode cdnNode = Factory.GetConfigNode("cdn");
                                _matchProtocol = MainUtil.GetBool(XmlUtil.GetAttribute("matchProtocol", cdnNode), false);
                            }
                        }
                    }
                    return _matchProtocol.GetValueOrDefault(false);
                }
            }
            


        }
    
}
