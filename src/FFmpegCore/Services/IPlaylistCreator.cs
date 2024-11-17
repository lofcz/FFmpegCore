using System.Collections.Generic;

namespace FFmpegCore.Services
{
    public interface IPlaylistCreator
    {
        string Create(IList<MetaData> metaData);
    }
}