namespace R3AIA.Services;

public interface IFileService
{
	Task<string> SaveImageAsync(IFormFile file, string folderName = "Uploads");
}

public class FileService : IFileService
{
	private readonly IWebHostEnvironment _env;
	private readonly IHttpContextAccessor _httpContextAccessor;

	public FileService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
	{
		_env = env;
		_httpContextAccessor = httpContextAccessor;
	}

	public async Task<string> SaveImageAsync(IFormFile file, string folderName = "Uploads")
	{
		var uploadsRootFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), folderName);
		if (!Directory.Exists(uploadsRootFolder))
		{
			Directory.CreateDirectory(uploadsRootFolder);
		}

		var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
		var filePath = Path.Combine(uploadsRootFolder, fileName);

		await using (var stream = new FileStream(filePath, FileMode.Create))
		{
			await file.CopyToAsync(stream);
		}

		var request = _httpContextAccessor.HttpContext!.Request;
		var baseUrl = $"{request.Scheme}://{request.Host}";

		var relativePath = $"{folderName}/{fileName}".Replace("\\", "/");
		return $"{baseUrl}/{relativePath}";
	}
}


