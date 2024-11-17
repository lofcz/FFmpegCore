using FFmpegCore.Services;
using FFmpegCore.Tests.Fixtures;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace FFmpegCore.Tests
{
    public class PlaylistCreatorTests : IClassFixture<MediaFileFixture>
    {
        public PlaylistCreatorTests(MediaFileFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        private readonly MediaFileFixture _fixture;
        private readonly ITestOutputHelper _output;

        [Fact]
        public async Task M3uPlaylistCreator_Creates_Valid_m3u8_Content()
        {
            Engine ffmpeg = new Engine(_fixture.FFmpegPath);
            MetaData meta1 = await ffmpeg.GetMetaDataAsync(_fixture.VideoFile, default).ConfigureAwait(false);
            MetaData meta2 = await ffmpeg.GetMetaDataAsync(_fixture.AudioFile, default).ConfigureAwait(false);

            _output.WriteLine(meta1?.ToString() ?? "-- KEIN META ! --");
            _output.WriteLine(meta2?.ToString() ?? "-- KEIN META ! --");

            Assert.NotNull(meta1);
            Assert.NotNull(meta2);

            string m3u8 = new M3uPlaylistCreator().Create(new[] { meta1, meta2 });

            Assert.NotNull(m3u8);

            string[] lines = m3u8.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

            Assert.True(lines.Length == 5);

            Assert.Equal("#EXTM3U", lines[0]);
            Assert.Equal("#EXTINF:5,SampleVideo_1280x720_1mb.mp4", lines[1]);
            Assert.Equal($"file:///{_fixture.VideoFile.FileInfo.FullName.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)}", lines[2]);
            Assert.Equal("#EXTINF:27,SampleAudio_0.4mb.mp3", lines[3]);
            Assert.Equal($"file:///{_fixture.AudioFile.FileInfo.FullName.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)}", lines[4]);
        }

        [Fact]
        public async Task XspfPlaylistCreator_Creates_Valid_Xml()
        {
            Engine ffmpeg = new Engine(_fixture.FFmpegPath);
            MetaData meta1 = await ffmpeg.GetMetaDataAsync(_fixture.VideoFile, default).ConfigureAwait(false);
            MetaData meta2 = await ffmpeg.GetMetaDataAsync(_fixture.AudioFile, default).ConfigureAwait(false);

            Assert.NotNull(meta1);
            Assert.NotNull(meta2);

            string xml = new XspfPlaylistCreator().Create(new[] { meta1, meta2 });

            Assert.NotNull(xml);
            Assert.NotEmpty(xml);

            using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("FFmpegCore.Tests.Resources.test.xspf"))
            using (StreamReader sr = new StreamReader(resource))
            {
                string xspf = sr.ReadToEnd();

                Assert.NotNull(xspf);

                string assemblyPath = Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
                string file1 = Path.GetRelativePath(assemblyPath, _fixture.VideoFile.FileInfo.FullName);
                string file2 = Path.GetRelativePath(assemblyPath, _fixture.AudioFile.FileInfo.FullName);

                Assert.NotNull(file1);
                Assert.NotNull(file2);
                Assert.Contains($"file:///{file1.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)}", xspf);
                Assert.Contains($"file:///{file2.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)}", xspf);
            }
        }
    }
}