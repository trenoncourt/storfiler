using System.Collections.Generic;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Storfiler.Options
{
    public class ApplicationOptions
    {
        public HostOptions Host { get; set; }
        
        public KestrelServerOptions Kestrel { get; set; }

        public IEnumerable<StorfilerOptions> Storfiler { get; set; }
    }
}