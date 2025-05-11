using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace SkillSwap.Pages.Admin
{
    [Authorize]
    public class ImageDebugModel : PageModel
    {
        private readonly IWebHostEnvironment _environment;

        public ImageDebugModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public string WebRootPath { get; set; }
        public string UploadsDirectory { get; set; }
        public bool DirectoryExists { get; set; }
        public List<FileInfo> UploadedFiles { get; set; } = new List<FileInfo>();

        public void OnGet()
        {
            WebRootPath = _environment.WebRootPath;
            UploadsDirectory = Path.Combine(WebRootPath, "uploads");
            DirectoryExists = Directory.Exists(UploadsDirectory);

            if (DirectoryExists)
            {
                var directoryInfo = new DirectoryInfo(UploadsDirectory);
                UploadedFiles = directoryInfo.GetFiles()
                    .OrderByDescending(f => f.LastWriteTime)
                    .Take(20)
                    .Select(f => new FileInfo
                    {
                        Name = f.Name,
                        Size = f.Length,
                        LastModified = f.LastWriteTime
                    })
                    .ToList();
            }
        }

        public void ProcessImageUpload(Stream imageStream, string filePath)
        {
            using (var image = Image.Load(imageStream))
            {
                // Resize if the image is too large
                if (image.Width > 1200 || image.Height > 1200)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(1200, 1200),
                        Mode = ResizeMode.Max
                    }));
                }

                // Save the resized image
                image.Save(filePath);
            }
        }

        public class FileInfo
        {
            public string Name { get; set; }
            public long Size { get; set; }
            public DateTime LastModified { get; set; }
        }
    }
}