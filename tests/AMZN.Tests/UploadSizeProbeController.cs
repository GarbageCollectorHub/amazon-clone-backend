using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AMZN.Tests;

// Принимает тестовую форму только после чтения её файлов. Ничего не записывает в БД или Blob.
[ApiController]
[Route("upload-size-probe")]
public class UploadSizeProbeController : ControllerBase
{
    private readonly UploadSizeProbeState _state;
    public UploadSizeProbeController(UploadSizeProbeState state) => _state = state;

    [HttpPost]
    public IActionResult Upload([FromForm] List<IFormFile> files)
    {
        _state.AcceptedForms++;
        return Ok(new { FileCount = files.Count, Bytes = files.Sum(file => file.Length) });
    }
}

public sealed class UploadSizeProbeState
{
    public int AcceptedForms { get; set; }
}
