using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Diagnostics;

namespace NTTData.SitecoreCDN.Util
{
    public class TimerReport : IDisposable
    {
        private HighResTimer _timer;
        private string _name;

        public TimerReport(string name)
        {
            _name = name;
            _timer = new HighResTimer(true);
        }

        public void Dispose()
        {
            _timer.Stop();
            System.Diagnostics.Debug.WriteLine(string.Format("{0} in {1}ms", _name, _timer.ElapsedTimeSpan.TotalMilliseconds));
        }
    }
}
