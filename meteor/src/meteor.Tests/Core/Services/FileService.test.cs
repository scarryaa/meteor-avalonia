using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Moq;
using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services.Tests
{
    public class FileServiceTests
    {
        private readonly IFileService _fileService;

        public FileServiceTests()
        {
            _fileService = new FileService();
        }

        [Fact]
        public async Task SaveFileAsync_ShouldWriteContentToFile()
        {
            // Arrange
            var tempFilePath = Path.GetTempFileName();
            var content = "Test content";

            try
            {
                // Act
                await _fileService.SaveFileAsync(tempFilePath, content);

                // Assert
                Assert.True(File.Exists(tempFilePath));
                var savedContent = await File.ReadAllTextAsync(tempFilePath);
                Assert.Equal(content, savedContent);
            }
            finally
            {
                // Cleanup
                File.Delete(tempFilePath);
            }
        }

        [Fact]
        public async Task OpenFileAsync_ShouldReadContentFromFile()
        {
            // Arrange
            var tempFilePath = Path.GetTempFileName();
            var content = "Test content";
            await File.WriteAllTextAsync(tempFilePath, content);

            try
            {
                // Act
                var result = await _fileService.OpenFileAsync(tempFilePath);

                // Assert
                Assert.Equal(content, result);
            }
            finally
            {
                // Cleanup
                File.Delete(tempFilePath);
            }
        }

        [Fact]
        public async Task SaveFileAsync_ShouldThrowExceptionForInvalidPath()
        {
            // Arrange
            var invalidPath = Path.Combine(Path.GetTempPath(), new string(Path.GetInvalidFileNameChars()));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _fileService.SaveFileAsync(invalidPath, "content"));
        }

        [Fact]
        public async Task OpenFileAsync_ShouldThrowExceptionForNonExistentFile()
        {
            // Arrange
            var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => _fileService.OpenFileAsync(nonExistentFile));
        }
    }
}