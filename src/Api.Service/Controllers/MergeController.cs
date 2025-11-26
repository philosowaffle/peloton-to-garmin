using System;
using Microsoft.AspNetCore.Mvc;
using PelotonToGarmin.Sync.Merge;

namespace PelotonToGarmin.Api.Service.Controllers
{
    [ApiController]
    [Route("api/merge")]
    public class MergeController : ControllerBase
    {
        private readonly MergeEngine _engine;

        public MergeController(MergeEngine engine)
        {
            _engine = engine;
        }

        [HttpPost("preview/{pelotonId}")]
        public IActionResult Preview(string pelotonId)
        {
            try
            {
                var result = _engine.PreviewMerge(pelotonId);
                return Ok(new { success = true, result.PelotonId, result.GarminActivityId, result.Score, result.MergedTcxPath, result.MergedFitPath, result.AutoApproved });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("approve")]
        public IActionResult Approve([FromBody] MergeResult preview)
        {
            try
            {
                var res = _engine.ApproveAndUpload(preview);
                return Ok(new { success = true, uploaded = res.GarminActivityId, note = res.Note });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
