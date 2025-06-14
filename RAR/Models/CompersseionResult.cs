using System;

namespace RAR.Helper
{
    public class CompressionResult
    {
        public string CompressedFilePath { get; set; }
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }


        public double CompressionRatio => OriginalSize == 0 ? 0 : (double)CompressedSize / OriginalSize;

  
        public string CompressionRatioPercent => $"{CompressionRatio:P2}";

        public string OriginalSizeFormatted => FormatBytes(OriginalSize);

   
        public string CompressedSizeFormatted => FormatBytes(CompressedSize);

    
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
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
