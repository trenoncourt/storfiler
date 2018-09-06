using System.Collections.Generic;

namespace Storfiler.Options
{
    public class StorfilerOptions
    {
        public string Resource { get; set; }

        public DiskPathsOptions DiskPaths { get; set; }

        public IEnumerable<StorfilerMethodOptions> Methods { get; set; }
    }

    public class DiskPathsOptions
    {
        public IEnumerable<string> Read { get; set; }

        public string Write { get; set; }
    }

    public class StorfilerMethodOptions
    {
        public string Verb { get; set; }

        public string Path { get; set; }

        public string Action { get; set; }

        public string Pattern { get; set; }

        public string Query { get; set; }

        public bool IsFullPath { get; set; }
    }
}