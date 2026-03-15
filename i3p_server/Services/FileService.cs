using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

public class FileService(Cloudinary cloudinary) // .NET 9 Primary Constructor
{
    public async Task<string> UploadFileAsync(IFormFile file, bool isImage)
    {
        using var stream = file.OpenReadStream();
        var uploadParams = isImage 
            ? new ImageUploadParams { File = new FileDescription(file.FileName, stream) }
            : new RawUploadParams { File = new FileDescription(file.FileName, stream) };

        var result = await cloudinary.UploadAsync(uploadParams);
        return result.SecureUrl.ToString();
    }
}