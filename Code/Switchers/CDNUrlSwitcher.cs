using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Common;

namespace NTTData.SitecoreCDN.Switchers
{
    public class CDNUrlSwitcher : Switcher<CDNUrlState>
    {
        public CDNUrlSwitcher(CDNUrlState state) : base(state) { }
    }
}
