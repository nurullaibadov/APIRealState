using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Infrastructure.Services
{
    public interface IFileService
    {
        Task<string> UploadImageAsync(IFormFile file, string folderName);
        Task<List<string>> UploadMultipleImagesAsync(List<IFormFile> files, string folderName);
        Task<bool> DeleteImageAsync(string imageUrl);
        bool IsValidImage(IFormFile file);
    }

    public class FileService : IFileService
    {
        private readonly string _uploadPath;
        private readonly long _maxFileSizeInBytes;
        private readonly string[] _allowedExtensions;

        public FileService(string uploadPath, long maxFileSizeInMB, string[] allowedExtensions)
        {
            _uploadPath = uploadPath;
            _maxFileSizeInBytes = maxFileSizeInMB * 1024 * 1024; // MB to bytes
            _allowedExtensions = allowedExtensions;

            // Upload klasörü yoksa oluştur
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        /// <summary>
        /// Tek resim yükle
        /// </summary>
        public async Task<string> UploadImageAsync(IFormFile file, string folderName)
        {
            // Validation
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty");
            }

            if (!IsValidImage(file))
            {
                throw new ArgumentException("Invalid file type or size");
            }

            // Klasör yolu oluştur
            var folderPath = Path.Combine(_uploadPath, folderName);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Unique dosya adı oluştur
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(folderPath, uniqueFileName);

            // Dosyayı kaydet
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Relative path döndür (database'de saklanacak)
            return $"/uploads/{folderName}/{uniqueFileName}";
        }

        /// <summary>
        /// Birden fazla resim yükle
        /// </summary>
        public async Task<List<string>> UploadMultipleImagesAsync(List<IFormFile> files, string folderName)
        {
            var uploadedFiles = new List<string>();

            foreach (var file in files)
            {
                try
                {
                    var filePath = await UploadImageAsync(file, folderName);
                    uploadedFiles.Add(filePath);
                }
                catch (Exception ex)
                {
                    // Hata logla ama devam et
                    Console.WriteLine($"Error uploading file {file.FileName}: {ex.Message}");
                }
            }

            return uploadedFiles;
        }

        /// <summary>
        /// Resmi sil
        /// </summary>
        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            try
            {
                // "/uploads/properties/abc.jpg" → "wwwroot/uploads/properties/abc.jpg"
                var filePath = Path.Combine("wwwroot", imageUrl.TrimStart('/'));

                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file {imageUrl}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Dosya validasyonu
        /// </summary>
        public bool IsValidImage(IFormFile file)
        {
            // Boyut kontrolü
            if (file.Length > _maxFileSizeInBytes)
            {
                return false;
            }

            // Extension kontrolü
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!_allowedExtensions.Contains(fileExtension))
            {
                return false;
            }

            // MIME type kontrolü (ekstra güvenlik)
            var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLower()))
            {
                return false;
            }

            return true;
        }
    }

}
