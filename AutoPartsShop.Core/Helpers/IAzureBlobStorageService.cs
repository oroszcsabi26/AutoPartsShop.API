using System.IO;
using System.Threading.Tasks;

namespace AutoPartShop.Core.Helpers
{
    public interface IAzureBlobStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName);
    }
}
