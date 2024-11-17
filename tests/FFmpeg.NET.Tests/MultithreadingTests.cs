﻿using FFmpeg.NET.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace FFmpeg.NET.Tests
{
    public class MultithreadingTests : IClassFixture<MediaFileFixture>
    {
        public MultithreadingTests(MediaFileFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        private readonly MediaFileFixture _fixture;
        private readonly ITestOutputHelper _output;


        [Fact]
        public void Converting_Multiple_Files_Simultaneously()
        {
            List<FileInfo> outputFiles = new List<FileInfo>();
            for (int i = 0; i < Environment.ProcessorCount; i++)
                outputFiles.Add(new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"MediaFiles\output{i}.mp4")));

            Engine ffmpeg = new Engine(_fixture.FFmpegPath);
            ffmpeg.Complete += (sender, args) => { _output.WriteLine("Complete: [{0} => {1}]", args.Input.Name, args.Output.Name); };
            ffmpeg.Progress += (sender, args) => { _output.WriteLine("Progress: {0}", args); };
            ffmpeg.Error += (sender, args) => { _output.WriteLine("Error: [{0} => {1}] ExitCode: {2}\n{3}", args.Input.Name, args.Output.Name, args.Exception.ExitCode, args.Exception); };
            ffmpeg.Data += (sender, args) => { _output.WriteLine("Data: {0} => {1} | {2}", args.Input.Name, args.Output.Name, args.Data); };

            List<Task> tasks = new List<Task>();
            foreach (FileInfo outputFile in outputFiles)
                tasks.Add(ffmpeg.ConvertAsync(_fixture.VideoFile, new OutputFile(outputFile), default));

            Task.WaitAll(tasks.ToArray());

            foreach (FileInfo outputFile in outputFiles)
            {
                Assert.True(File.Exists(outputFile.FullName));
                outputFile.Delete();
                Assert.False(File.Exists(outputFile.FullName));
            }
        }

        [Fact]
        public void Multiple_FFmpeg_Instances_At_Once_Do_Not_Throw_Exception()
        {
            Task<MetaData> task1 = new Engine(_fixture.FFmpegPath).GetMetaDataAsync(_fixture.VideoFile, default);
            Task<MetaData> task2 = new Engine(_fixture.FFmpegPath).GetMetaDataAsync(_fixture.VideoFile, default);
            Task<MetaData> task3 = new Engine(_fixture.FFmpegPath).GetMetaDataAsync(_fixture.VideoFile, default);
            Task.WaitAll(task1, task2, task3);
        }
    }
}