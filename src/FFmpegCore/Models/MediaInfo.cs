using System;

namespace FFmpegCore.Models
{
	public class MediaInfo
	{
		public MediaInfo(double bitrate, TimeSpan totalDuration)
		{
			Bitrate = bitrate;
			TotalDuration = totalDuration;
		}

		public double Bitrate { get; }
		public TimeSpan TotalDuration { get; internal set; }
	}
}