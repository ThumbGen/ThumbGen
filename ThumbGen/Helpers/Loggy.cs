using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.Threading;
using Microsoft.Win32;
using ThumbGen.Core;

namespace ThumbGen
{
    public class Loggy
    {
        public static Logger Logger
        {
            get
            {
                return ThumbGen.Core.Loggy.Logger;
            }
        }

    }
}
