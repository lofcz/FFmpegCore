using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FFmpegCore.Services
{
    public class M3uPlaylistCreator : IPlaylistCreator
    {
        public string Create(IList<MetaData> metaData)
        {
            if (metaData == null)
                throw new ArgumentException(null, nameof(metaData));

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#EXTM3U");
            foreach (MetaData meta in metaData)
            {
                sb.AppendLine($"#EXTINF:{(int) meta.Duration.TotalSeconds},{meta.FileInfo.Name}");
                sb.AppendLine($"file:///{meta.FileInfo.FullName.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)}");
            }

            return sb.ToString();
        }
    }
}