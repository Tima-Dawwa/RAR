using System.Collections.Generic;

namespace RAR.Helper
{
    public class CompressionResult
    {
        public string CompressedFilePath { get; set; }
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }
        public bool IsEncrypted { get; set; }

        public double CompressionRatio
        {
            get
            {
                if (OriginalSize == 0) return 0;
                return ((double)(OriginalSize - CompressedSize) / OriginalSize) * 100;
            }
        }

        public string CompressionRatioPercent
        {
            get
            {
                return string.Format("{0:P2}", CompressionRatio / 100);
            }
        }
    }

    public class FolderCompressionResult
    {
        public string OriginalFolderPath { get; set; }
        public string CompressedFolderPath { get; set; }
        public List<CompressionResult> FileResults { get; set; }
        public long TotalOriginalSize { get; set; }
        public long TotalCompressedSize { get; set; }
        public int FileCount { get; set; }
        public bool IsEncrypted { get; set; }

        public double OverallCompressionRatio
        {
            get
            {
                if (TotalOriginalSize == 0) return 0;
                return ((double)(TotalOriginalSize - TotalCompressedSize) / TotalOriginalSize) * 100;
            }
        }

        public string OverallCompressionRatioPercent
        {
            get
            {
                return string.Format("{0:P2}", OverallCompressionRatio / 100);
            }
        }

        // Keep the old property for backward compatibility
        public double CompressionRatio => OverallCompressionRatio;
    }
}