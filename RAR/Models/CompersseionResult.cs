using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAR.Helper
{
    public class CompressionResult
    {
        public string CompressedFilePath { get; set; }
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }

        public double CompressionRatio => (double)CompressedSize / OriginalSize;

    }
}
