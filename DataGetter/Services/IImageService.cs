
namespace DataGetter.Services
{
    internal interface IImageService
    {
        MediaFile? GetImage(string path);
        IEnumerable<Media> GetImages();
    }
}