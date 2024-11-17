using FFmpegCore.Extensions;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace FFmpegCore.Tests
{
    public class StringExtensionsTest
    {
        [Fact]
        public void Should_Get_FileExists_True_On_This_File()
        {
            // Arrange
            string testFolder = GetTestFolder();
            string thisFile = Path.Combine(testFolder, nameof(StringExtensionsTest) + ".cs");

            // Act
            bool fileExists = File.Exists(thisFile);

            // Assert
            Assert.True(fileExists);
        }

        [Fact]
        public void Should_Get_FullPath_To_Existing_File()
        {
            // Arrange
            string testFolder = GetTestFolder();
            string thisFile = Path.Combine(testFolder, nameof(StringExtensionsTest) + ".cs");

            // Act
            bool fileExists = thisFile.TryGetFullPath(out string fullPath);
            bool fileExistsVerified = File.Exists(fullPath);

            // Assert
            Assert.True(fileExists);
            Assert.True(fileExistsVerified);
        }

        [Fact]
        public void Should_Not_Get_FullPath_To_NonExisting_File()
        {
            // Arrange
            string testFolder = GetTestFolder();
            string bogusFile = Path.Combine(testFolder, "bogusFile.txt");

            // Act
            bool fileExists = bogusFile.TryGetFullPath(out string fullPath);
            bool fileExistsVerified = File.Exists(fullPath);

            // Assert
            Assert.False(fileExists);
            Assert.False(fileExistsVerified);
        }

        [Fact]
        public void Should_Get_FullPath_When_Directory_Is_In_Path()
        {
            // Arrange
            string testFolder = GetTestFolder();
            string testFileName = "existsInPathEnvFile.deleteMe";
            string testFileFullPath = Path.Combine(testFolder, testFileName);
            string pathVariable = System.Environment.GetEnvironmentVariable("PATH");
            string newPathVariable = pathVariable + $";{testFolder}";
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Process;

            Environment.SetEnvironmentVariable("PATH", newPathVariable, target);
            using (File.Create(testFileFullPath)) { }

            // Act
            bool fileExists = testFileName.TryGetFullPath(out string fullPath);
            bool fileExistsVerified = File.Exists(fullPath);

            // Cleanup
            Environment.SetEnvironmentVariable("PATH", pathVariable, target);
            File.Delete(testFileFullPath);

            // Assert
            Assert.True(fileExists);
            Assert.True(fileExistsVerified);
        }

        [Fact]
        public void Should_Not_Get_FullPath_When_Directory_Is__In_Path_But_File_Is_Not()
        {
            // Arrange
            string testFolder = GetTestFolder();
            string testFileName = "notInPathEnvFile.txt";
            string orgPathVariable = Environment.GetEnvironmentVariable("PATH");
            string newPathVariable = orgPathVariable + $";{testFolder}";
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Process;

            Environment.SetEnvironmentVariable("PATH", newPathVariable, target);

            // Act
            bool fileExists = testFileName.TryGetFullPath(out string fullPath);
            bool fileExistsVerified = File.Exists(fullPath);

            // Cleanup
            Environment.SetEnvironmentVariable("PATH", orgPathVariable, target);

            // Assert
            Assert.False(fileExists);
            Assert.False(fileExistsVerified);
        }

        private static string GetTestFolder()
        {
            string startupPath = AppContext.BaseDirectory;
            string[] pathItems = startupPath.Split(Path.DirectorySeparatorChar);
            int pos = pathItems.Reverse().ToList().FindIndex(x => string.Equals("bin", x));
            string projectPath = string.Join(Path.DirectorySeparatorChar.ToString(), pathItems.Take(pathItems.Length - pos - 1));
            return projectPath;
        }
    }
}