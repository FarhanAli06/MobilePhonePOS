using Empire.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Empire.API.Controllers;

[Authorize]
[Route("api/backup")]
public class BackupController : BaseApiController
{
    private readonly DatabaseBackupState _state;
    public BackupController(DatabaseBackupState state) => _state = state;

    [HttpGet("status")]
    public IActionResult Status() => SuccessResponse(new
    {
        lastSuccessfulUtc = _state.LastSuccessfulUtc,
        lastAttemptUtc = _state.LastAttemptUtc,
        status = _state.Status,
        fileName = _state.FileName,
        schedule = "Every 24 hours"
    });
}
