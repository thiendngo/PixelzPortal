
using Microsoft.AspNetCore.Mvc;
using PixelzPortal.Application.Interfaces;

namespace PixelzPortal.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttachmentsController : ControllerBase
    {
        private readonly IAttachmentService _attachmentService;

        public AttachmentsController(IAttachmentService attachmentService)
        {
            _attachmentService = attachmentService;
        }

        // GET: api/attachments/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMetadata(Guid id)
        {
            var metadata = await _attachmentService.GetAttachmentMetadataAsync(id);
            if (metadata == null)
                return NotFound();

            return Ok(metadata);
        }

        // GET: api/attachments/{id}/download
        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(Guid id)
        {
            var attachment = await _attachmentService.GetAttachmentAsync(id);
            if (attachment == null)
                return NotFound();

            // Assuming OrderAttachment has Data, FileType, FileName
            return File(attachment.Data, attachment.FileType, attachment.FileName);
        }

        // DELETE: api/attachments/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _attachmentService.DeleteAttachmentAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }
    }

}
