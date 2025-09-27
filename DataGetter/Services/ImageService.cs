using Microsoft.Extensions.Logging;
using Renci.SshNet;
using SixLabors.ImageSharp;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Web;

namespace DataGetter.Services
{
    internal class ImageService : IImageService
    {
        private Settings _settings;
        private readonly ILogger<ImageService> _logger;

        public ImageService(Settings settings, ILogger<ImageService> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public IEnumerable<Media> GetImages()
        {
            using SftpClient client = new SftpClient(new PasswordConnectionInfo(_settings.Image.Host, _settings.Image.Username, _settings.Image.Password));
            client.Connect();

            var media = new List<Media>();

            foreach (var path in _settings.Image.Paths)
            {
                if (client.Exists(path))
                {
                    media.AddRange(client.ListDirectory(path)
                                         .Where(i => !i.IsDirectory && i.FullName.EndsWith("jpg"))
                                         .Select(s => new Media { Filename = s.FullName, Id = HttpUtility.UrlEncode(s.FullName) }));
                }
            }

            client.Disconnect();

            _logger.LogInformation($"Found {media.Count} images");
            return media;
        }

        public MediaFile? GetImage(string path)
        {
            using SftpClient client = new SftpClient(new PasswordConnectionInfo(_settings.Image.Host, _settings.Image.Username, _settings.Image.Password));
            client.Connect();
            
            if (!client.Exists(path))
            {
                _logger.LogError($"Failed to find {path}");
                return null;
            }

            using var memoryStream = new MemoryStream();
            client.DownloadFile(path, memoryStream);

            var image = Image.Load(memoryStream.ToArray());

            DateTime createdDate = DateTime.MinValue;
            var exifProfile = image.Metadata.ExifProfile;
            if (exifProfile != null)
            {
                if (exifProfile.TryGetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.DateTimeOriginal, out var exifValue))
                {
                    var dateString = exifValue?.GetValue() as string;
                    if (!string.IsNullOrEmpty(dateString))
                    {
                        if (DateTime.TryParse(dateString, out DateTime parsedDate))
                            createdDate = parsedDate;
                        if (DateTime.TryParseExact(dateString, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                            createdDate = parsedDate;
                    }
                }
            }

            return new MediaFile
            {
                Filename = path,
                Data = memoryStream.ToArray(),
                Width = image.Width,
                Height = image.Height,
                CreatedDate = createdDate
            };
        }

    }
}
