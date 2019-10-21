using System.Collections.Generic;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Storfiler.AspNetCore;

namespace Storfiler.Options
{
    public class ApplicationOptions
    {
        public HostOptions Host { get; set; }
        
        public KestrelServerOptions Kestrel { get; set; }

        public StorfilerOptions Storfiler { get; set; }
    }
}