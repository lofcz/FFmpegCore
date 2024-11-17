﻿using FFmpeg.NET.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FFmpeg.NET
{
    /// <summary>
    ///     Contains all Regex tasks
    /// </summary>
    public static class RegexEngine
    {
        /// <summary>
        ///     Dictionary containing every Regex test.
        /// </summary>
        internal static readonly Dictionary<Find, Regex> _index = new Dictionary<Find, Regex>()
        {
            { Find.BitRate, new Regex(@"([0-9]*)\s*kb/s") },
            { Find.ClipBitrate, new Regex(@"bitrate: ([0-9]*)\s*kb/s ") },
            { Find.Duration, new Regex(@"Duration: ([^,]*), ") },
            { Find.ConvertProgressFrame, new Regex(@"frame=\s*([0-9]*)") },
            { Find.ConvertProgressFps, new Regex(@"fps=\s*([0-9]*\.?[0-9]*?)") },
            { Find.ConvertProgressSize, new Regex(@"size=\s*([0-9]*)kB") },
            { Find.ConvertProgressFinished, new Regex(@"Lsize=\s*([0-9]*)kB") },
            { Find.ConvertProgressTime, new Regex(@"time=\s*([^ ]*)") },
            { Find.ConvertProgressBitrate, new Regex(@"bitrate=\s*([0-9]*\.?[0-9]*?)kbits/s") },
            { Find.MetaAudio, new Regex(@"(Stream\s*#[0-9]*:[0-9]*\(?[^\)]*?\)?: Audio:.*)") },
            { Find.AudioFormatHzChannel, new Regex(@"Audio:\s*([^,]*),\s([^,]*),\s([^,]*)") },
            { Find.MetaVideo, new Regex(@"(Stream\s*#[0-9]*:[0-9]*\(?[^\)]*?\)?: Video:.*)") },
            { Find.VideoFormatColorSize, new Regex(@"Video:\s*([^,]*),\s*((?:[^,]*,?[^,]*?)(?:\(.*\))?),\s*(?=[0-9]*x[0-9]*)([0-9]*x[0-9]*)") },
            { Find.VideoFps, new Regex(@"([0-9\.]*)\s*tbr") }
        };

        /// <summary>
        ///     <para> ---- </para>
        ///     <para>Establishes whether the data contains progress information.</para>
        /// </summary>
        /// <param name="data">Event data from the FFmpeg console.</param>
        /// <param name="progressData">
        ///     <para>If successful, outputs a <see cref="ProgressData" /> which is </para>
        ///     <para>generated from the data. </para>
        /// </param>
        internal static bool IsProgressData(string data, out ProgressData progressData)
        {
            progressData = null;

            Match matchFrame = _index[Find.ConvertProgressFrame].Match(data);
            Match matchFps = _index[Find.ConvertProgressFps].Match(data);
            Match matchSize = _index[Find.ConvertProgressSize].Match(data);
            Match matchTime = _index[Find.ConvertProgressTime].Match(data);
            Match matchBitrate = _index[Find.ConvertProgressBitrate].Match(data);

            if (!matchTime.Success)
                return false;

            TimeSpanLargeTryParse(matchTime.Groups[1].Value, out TimeSpan processedDuration);

            long? frame = GetLongValue(matchFrame);
            double? fps = GetDoubleValue(matchFps);
            int? sizeKb = GetIntValue(matchSize);
            double? bitrate = GetDoubleValue(matchBitrate);

            progressData = new ProgressData(processedDuration, TimeSpan.Zero, frame, fps, sizeKb, bitrate);

            return true;
        }
        internal static bool IsMediaInfo(string data, out MediaInfo mediaInfo)
        {
            mediaInfo = null;

            if (data == null || data == "") return false;
            Match matchBitrate = _index[Find.BitRate].Match(data);
            Match matchDuration = _index[Find.Duration].Match(data);

            if (!matchBitrate.Success || !matchDuration.Success)
                return false;

            TimeSpanLargeTryParse(matchDuration.Groups[1].Value, out TimeSpan clipDuration);

            double? bitrate = GetDoubleValue(matchBitrate);
            if (bitrate.HasValue)
            {
                mediaInfo = new MediaInfo(bitrate.Value, clipDuration);
                return true;
            }
            else
            {
                return false;
            }
        }

        // Parse timespan string as returned by ffmpeg. No days, allow hours to
        // exceed 23.
        internal static bool TimeSpanLargeTryParse(string str, out TimeSpan result)
        {
            result = TimeSpan.Zero;

            // Process hours.
            int start = 0;
            int end = str.IndexOf(':', start);
            if (end < 0)
                return false;
            if (!int.TryParse(str[start..end], out int hours))
                return false;

            // Process minutes
            start = end + 1;
            end = str.IndexOf(':', start);
            if (end < 0)
                return false;
            if (!int.TryParse(str[start..end], out int minutes))
                return false;

            // Process seconds
            start = end + 1;
            // ffmpeg doesnt respect the computers culture
            if (!double.TryParse(str[start..], NumberStyles.Number, CultureInfo.InvariantCulture, out double seconds))
                return false;

            result = new TimeSpan(0, hours, minutes, 0, (int)Math.Round(seconds * 1000.0));
            return true;
        }

        private static long? GetLongValue(Match match)
            => long.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out long result)
                ? (long?)result
                : null;

        private static double? GetDoubleValue(Match match)
            => double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result)
                ? (double?)result
                : null;

        private static int? GetIntValue(Match match)
            => int.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out int result)
                ? (int?)result
                : null;

        public static void TestVideo(string data, FFmpegParameters engine)
        {
            Match matchMetaVideo = _index[Find.MetaVideo].Match(data);

            if (!matchMetaVideo.Success)
                return;

            string fullMetadata = matchMetaVideo.Groups[1].ToString();

            GroupCollection matchVideoFormatColorSize = _index[Find.VideoFormatColorSize].Match(fullMetadata).Groups;
            GroupCollection matchVideoFps = _index[Find.VideoFps].Match(fullMetadata).Groups;
            Match matchVideoBitRate = _index[Find.BitRate].Match(fullMetadata);

            IInputArgument input = engine.Input;
            if (input.MetaData == null)
                input.MetaData = new MetaData();

            if (input.MetaData.VideoData == null)
                input.MetaData.VideoData = new MetaData.Video
                {
                    Format = matchVideoFormatColorSize[1].ToString(),
                    ColorModel = matchVideoFormatColorSize[2].ToString(),
                    FrameSize = matchVideoFormatColorSize[3].ToString(),
                    Fps = matchVideoFps[1].Success && !string.IsNullOrEmpty(matchVideoFps[1].ToString()) ? Convert.ToDouble(matchVideoFps[1].ToString(), new CultureInfo("en-US")) : 0,
                    BitRateKbs =
                        matchVideoBitRate.Success
                            ? (int?)Convert.ToInt32(matchVideoBitRate.Groups[1].ToString())
                            : null
                };
        }

        public static void TestAudio(string data, FFmpegParameters engine)
        {
            Match matchMetaAudio = _index[Find.MetaAudio].Match(data);

            if (!matchMetaAudio.Success)
                return;

            string fullMetadata = matchMetaAudio.Groups[1].ToString();

            GroupCollection matchAudioFormatHzChannel = _index[Find.AudioFormatHzChannel].Match(fullMetadata).Groups;
            GroupCollection matchAudioBitRate = _index[Find.BitRate].Match(fullMetadata).Groups;
            IInputArgument input = engine.Input;
            if (input.MetaData == null)
                input.MetaData = new MetaData();

            if (input.MetaData.AudioData == null)
                input.MetaData.AudioData = new MetaData.Audio
                {
                    Format = matchAudioFormatHzChannel[1].ToString(),
                    SampleRate = matchAudioFormatHzChannel[2].ToString(),
                    ChannelOutput = matchAudioFormatHzChannel[3].ToString(),
                    BitRateKbs = !string.IsNullOrWhiteSpace(matchAudioBitRate[1].ToString()) ? Convert.ToInt32(matchAudioBitRate[1].ToString()) : 0
                };
        }

        internal enum Find
        {
            AudioFormatHzChannel,
            ConvertProgressBitrate,
            ConvertProgressFps,
            ConvertProgressFrame,
            ConvertProgressSize,
            ConvertProgressFinished,
            ConvertProgressTime,
            Duration,
            ClipBitrate,
            MetaAudio,
            MetaVideo,
            BitRate,
            VideoFormatColorSize,
            VideoFps
        }
    }
}
