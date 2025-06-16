using System;

namespace RAR.Helper
{
    public class CompressionResult
    {
        public string CompressedFilePath { get; set; }
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }

        public double CompressionRatio
        {
            get
            {
                return OriginalSize > 0 ? (double)(OriginalSize - CompressedSize) / OriginalSize * 100 : 0;
            }
        }

        public string CompressionRatioPercent
        {
            get
            {
                return string.Format("{0:P2}", CompressionRatio / 100);
            }
        }

        public string OriginalSizeFormatted
        {
            get
            {
                return FormatBytes(OriginalSize);
            }
        }

        public string CompressedSizeFormatted
        {
            get
            {
                return FormatBytes(CompressedSize);
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }
    }
}