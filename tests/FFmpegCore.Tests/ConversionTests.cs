using FFmpegCore.Events;
using FFmpegCore.Tests.Fixtures;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace FFmpegCore.Tests
{
    public class ConversionTests : IClassFixture<MediaFileFixture>
    {
        public ConversionTests(MediaFileFixture fixture, ITestOutputHelper outputHelper)
        {
            _fixture = fixture;
            _outputHelper = outputHelper;
        }

        private readonly MediaFileFixture _fixture;
        private readonly ITestOutputHelper _outputHelper;
        private TimeSpan _processedDuration = new TimeSpan();

        // Check that progress events are received
        [Fact]
        public async Task FFmpeg_Invokes_ProgressEvent()
        {
            Engine ffmpeg = new Engine(_fixture.FFmpegPath);

            await MetaDataTests.CreateLongAudioFile(ffmpeg, _fixture.AudioFile).ConfigureAwait(false);

            FileInfo audioFileInfo = new FileInfo("LongAudio.mp3");
            InputFile audioFile = new InputFile(audioFileInfo);
            ffmpeg.Progress += Ffmpeg_Progress;

            OutputFile output = new OutputFile("LongAudio.aif");
            await ffmpeg.ConvertAsync(audioFile, output, default).ConfigureAwait(false);

            ffmpeg.Progress -= Ffmpeg_Progress;
            output.FileInfo.Delete();

            Assert.InRange(_processedDuration.TotalHours, 30.0, 40.0);
        }

        private void Ffmpeg_Progress(object sender, ConversionProgressEventArgs e)
        {
            _processedDuration = e.ProcessedDuration;
            Debug.WriteLine($"Processed duration hours: {_processedDuration.TotalHours}");
        }

        [Fact]
        public async Task FFmpeg_Invokes_ConversionCompleteEvent()
        {
            OutputFile output = new OutputFile(new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"MediaFiles\conversionTest.mp4")));
            Engine ffmpeg = new Engine(_fixture.FFmpegPath);

            Assert.RaisedEvent<ConversionCompleteEventArgs> e = await Assert.RaisesAsync<ConversionCompleteEventArgs>(
                x => ffmpeg.Complete += x,
                x => ffmpeg.Complete -= x,
                async () => await ffmpeg.ConvertAsync(_fixture.VideoFile, output, default)
            ).ConfigureAwait(false);

            Assert.True(File.Exists(output.FileInfo.FullName));
            output.FileInfo.Delete();
            Assert.False(File.Exists(output.FileInfo.FullName));

            Assert.NotNull(e);
            Assert.Equal(e.Sender, ffmpeg);
            Assert.Equal(_fixture.VideoFile.FileInfo.FullName, e.Arguments.Input.Name);
            Assert.Equal(output.FileInfo.FullName, e.Arguments.Output.Name);
        }

        [Fact]
        public async Task FFmpeg_Should_Throw_Exception_On_Invalid_OutputFile()
        {
            Engine ffmpeg = new Engine(_fixture.FFmpegPath);
            OutputFile output = new OutputFile("test.txt");
            InputFile input = _fixture.VideoFile;

            Assert.RaisedEvent<ConversionErrorEventArgs> e = await Assert.RaisesAsync<ConversionErrorEventArgs>(
                x => ffmpeg.Error += x,
                x => ffmpeg.Error -= x,
                async () => await ffmpeg.ConvertAsync(input, output, default))
                .ConfigureAwait(false);

            Assert.NotNull(e);
            Assert.Equal(e.Sender, ffmpeg);
            Assert.Equal(input, e.Arguments.Input);
            Assert.Equal(output, e.Arguments.Output);
            Assert.Equal(1, e.Arguments.Exception.ExitCode);
        }

        [Fact]
        public async Task FFmpeg_Invokes_ProgressEvent_With_Segment_Option()
        {
            Engine ffmpeg = new Engine(_fixture.FFmpegPath);

            // Progress messages from ffmpeg usually look like this:
            //      size=       0kB time=-577014:32:22.77 bitrate=N/A speed=N/
            // But when some options (such as -f segment) are used, you do not get the bitrate 
            // and size.  In this case you will see
            //      size=N/A time=00:11:00.11 bitrate=N/A speed=1.32e+03x
            // Test for this case using -f segment option.
            // Concatenate two copies of the sample audio file together to make sure the input is long enough
            // to generate progress messages. Split the result into 20 second segments. 

            string fn = $"-i \"{_fixture.AudioFile.FileInfo.FullName}\"";
            string options = $"{fn} {fn} -filter_complex \"[0:0][1:0]concat=n=2:v=0:a=1[out]\" -map \"[out]\" -f segment -segment_time 20 Split%1d.mp3";

            Assert.RaisedEvent<ConversionProgressEventArgs> e = await Assert.RaisesAsync<ConversionProgressEventArgs>(
                x => ffmpeg.Progress += x,
                x => ffmpeg.Progress -= x,
                async () => await ffmpeg.ExecuteAsync(options, default)
            ).ConfigureAwait(false);

            File.Delete("Split0.mp3");
            File.Delete("Split1.mp3");
            File.Delete("Split2.mp3");
        }

        [Fact]
        public async Task FFmpeg_Raises_ProgressEvent()
        {
            OutputFile output = new OutputFile(new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"MediaFiles\conversionTest.mp4")));
            Engine ffmpeg = new Engine(_fixture.FFmpegPath);

            Assert.RaisedEvent<ConversionProgressEventArgs> e = await Assert.RaisesAsync<ConversionProgressEventArgs>(
                x => ffmpeg.Progress += x,
                x => ffmpeg.Progress -= x,
                async () => await ffmpeg.ConvertAsync(_fixture.VideoFile, output, default)
            ).ConfigureAwait(false);

            Assert.True(File.Exists(output.FileInfo.FullName));
            output.FileInfo.Delete();
            Assert.False(File.Exists(output.FileInfo.FullName));

            Assert.NotNull(e);
            Assert.Equal(e.Sender, ffmpeg);
            Assert.Equal(_fixture.VideoFile.FileInfo.FullName, e.Arguments.Input.Name);
            Assert.Equal(output.FileInfo.FullName, e.Arguments.Output.Name);
        }

        [Fact]
        public async Task FFmpeg_Raises_DataEvent()
        {
            OutputFile output = new OutputFile(new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"MediaFiles\conversionTest.mp4")));
            Engine ffmpeg = new Engine(_fixture.FFmpegPath);

            Assert.RaisedEvent<ConversionDataEventArgs> e = await Assert.RaisesAsync<ConversionDataEventArgs>(
                x => ffmpeg.Data += x,
                x => ffmpeg.Data -= x,
                async () => await ffmpeg.ConvertAsync(_fixture.VideoFile, output, default)
            ).ConfigureAwait(false);

            Assert.True(File.Exists(output.FileInfo.FullName));
            output.FileInfo.Delete();
            Assert.False(File.Exists(output.FileInfo.FullName));

            Assert.NotNull(e);
            Assert.Equal(e.Sender, ffmpeg);
            Assert.Equal(_fixture.VideoFile.FileInfo.FullName, e.Arguments.Input.Name);
            Assert.Equal(output.FileInfo.FullName, e.Arguments.Output.Name);
        }

        [Fact]
        public async Task FFmpeg_Should_Process_Conversion_Using_Stream_Input()
        {
            OutputFile output = new OutputFile(new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"MediaFiles\conversionTest.flv")));
            Engine ffmpeg = new Engine(_fixture.FFmpegPath);

             FileStream stream = new FileStream(_fixture.FlvVideoFile.FileInfo.FullName, FileMode.Open, FileAccess.Read);
             await using StreamInput input = new StreamInput(stream, true);

            Task<MediaFile> ffmpegTask = ffmpeg.ConvertAsync(input, output, CancellationToken.None);
            await ffmpegTask;

            Assert.True(File.Exists(output.FileInfo.FullName));
            output.FileInfo.Delete();
            Assert.False(File.Exists(output.FileInfo.FullName));
        }

        [Fact]
        public async Task FFmpeg_Should_Process_Conversion_Using_StandardInputWriter_Input()
        {
            OutputFile output = new OutputFile(new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"MediaFiles\conversionTest.flv")));
            Engine ffmpeg = new Engine(_fixture.FFmpegPath);
            StandardInputWriter input = new StandardInputWriter();

            Task<MediaFile> ffmpegTask = ffmpeg.ConvertAsync(input, output, CancellationToken.None);
            await PopulateStdInAsync(_fixture.FlvVideoFile.FileInfo, input);
            await ffmpegTask;

            Assert.True(File.Exists(output.FileInfo.FullName));
            output.FileInfo.Delete();
            Assert.False(File.Exists(output.FileInfo.FullName));
        }

        private async Task PopulateStdInAsync(FileInfo file, StandardInputWriter input)
        {
            await using FileStream stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);

            byte[] buffer = new byte[64 * 1024];
            int bytesRead = 0;
            do
            {
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                await input.WriteAsync(buffer, 0, bytesRead);
            } while (bytesRead > 0 && input.IsOpen);

            input.Close();
        }
    }
}