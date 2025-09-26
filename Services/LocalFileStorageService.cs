namespace ECommerce.Services
{
    public class LocalFileStorageService : IFileService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private const string UploadsFolder = "uploads"; // Default folder name

        public LocalFileStorageService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<string?> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
                return null;

            var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}{extension}";
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, UploadsFolder);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"uploads/{fileName}"; 
        }

        public Task<bool> DeleteFileAsync(string fileName)
        {
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, UploadsFolder, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}
