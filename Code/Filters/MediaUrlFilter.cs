using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using Sitecore;
using HtmlAgilityPack;
using Sitecore.Text;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Web;
using NTTData.SitecoreCDN.Configuration;
using NTTData.SitecoreCDN.Util;

namespace NTTData.SitecoreCDN.Filters
{

    /// <summary>
    /// A filter stream that allows the replacing of img/script src attributes (and link tag's href attribute) 
    /// with CDN appended urls
    /// 
    /// i.e.   "~/media/path/to/file.ashx?w=400&h=200"  becomes "http://mycdnhostname/~/media/path/to/file.ashx?w=400&h=200&v=2&d=20130101T000000"
    /// 
    /// </summary>
    public class MediaUrlFilter : Stream
    {
        private Stream _responseStream;
        private long _position;
        private StringBuilder _sb;
        private bool _isComplete;

        public MediaUrlFilter(Stream inputStream)
        {
            _responseStream = inputStream;
            _sb = new StringBuilder();
            _isComplete = false;
        }


        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
            // if the stream wasn't completed by Write, output the contents of the inner stream first
            if (!_isComplete)
            {
                byte[] data = UTF8Encoding.UTF8.GetBytes(_sb.ToString());
                _responseStream.Write(data, 0, data.Length);
            }
            _responseStream.Flush();
        }

        public override void Close()
        {
            _responseStream.Close();
        }

        public override long Length
        {
            get { return 0; }
        }

        public override long Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _responseStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _responseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _responseStream.SetLength(value);
        }


        /// <summary>
        /// This Method buffers the original Write payloads until the end of the end [/html] tag
        /// when replacement occurs
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            // preview the contents of the payload
            string content = UTF8Encoding.UTF8.GetString(buffer, offset, count);

            Regex eof = new Regex("</html>", RegexOptions.IgnoreCase);
            // if the content contains </html> we know we're at the end of the line
            // otherwise append the contents to the stringbuilder
            if (!eof.IsMatch(content))
            {
                if (_isComplete)
                {
                    _responseStream.Write(buffer, offset, count);
                }
                else
                {
                    _sb.Append(content);
                }
            }
            else
            {
                _sb.Append(content.Substring(0, content.IndexOf("</html>") + 7));

                string extra = content.Substring(content.IndexOf("</html>") + 7);

                try
                {
                    using (new TimerReport("replaceMediaUrls"))
                    {
                        // parse complete document into HtmlDocument
                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(_sb.ToString());

                        if (CDNSettings.DebugParser)
                        {
                            var parseErrors = doc.ParseErrors;
                            if (parseErrors != null)
                                parseErrors = parseErrors.Where(pe => pe.Code == HtmlParseErrorCode.EndTagInvalidHere || pe.Code == HtmlParseErrorCode.TagNotClosed || pe.Code == HtmlParseErrorCode.TagNotOpened);

                            if (parseErrors != null && parseErrors.Any())
                            {
                                StringBuilder sb = new StringBuilder();
                                foreach (var parseError in parseErrors)
                                {
                                    sb.AppendLine(string.Format("PARSE ERROR: {0}", parseError.Reason));
                                    sb.AppendLine(string.Format("Line: {0} Position: {1}", parseError.Line, parseError.LinePosition));
                                    sb.AppendLine(string.Format("Source: {0}", parseError.SourceText));
                                    sb.AppendLine("");
                                }

                                Log.Error(string.Format("CDN Url Parsing Error - URL: {0} {1} {2}", WebUtil.GetRawUrl(), Environment.NewLine, sb.ToString()), this);
                            }
                        }
                        // replace appropriate urls
                        CDNManager.ReplaceMediaUrls(doc);

                        StreamWriter writer = new StreamWriter(_responseStream);
                        doc.Save(writer);
                        writer.Flush();
                    }
                    _isComplete = true;
                }
                catch (Exception ex)
                {
                    Log.Error("CDN MediaURL Filter Error", ex, this);
                }

                if (!string.IsNullOrEmpty(extra))
                {
                    byte[] data = UTF8Encoding.UTF8.GetBytes(extra);
                    _responseStream.Write(data, 0, data.Length);
                }

            }
        }





    }
}
