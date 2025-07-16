
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixelzPortal.Infrastructure.Persistence;

namespace PixelzPortal.Api.Controllers
{



    [Route("api/[controller]")]
    [ApiController]
    public class AttachmentsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AttachmentsController(AppDbContext context)
        {
            _db = context;
        }

        // GET: api/attachments/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMetadata(Guid id)
        {
            var attachment = await _db.OrderAttachments
                .Where(a => a.AttachmentId == id)
                .Select(a => new
                {
                    a.AttachmentId,
                    a.FileName,
                    a.FileType,
                    a.CreatedAt,
                    a.OrderId
                })
                .FirstOrDefaultAsync();

            if (attachment == null)
                return NotFound();

            return Ok(attachment);
        }

        // GET: api/attachments/{id}/download
        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(Guid id)
        {
            var attachment = await _db.OrderAttachments.FindAsync(id);

            if (attachment == null)
                return NotFound();

            return File(attachment.Data, attachment.FileType, attachment.FileName);
        }

        // DELETE: api/attachments/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var attachment = await _db.OrderAttachments.FindAsync(id);

            if (attachment == null)
                return NotFound();

            _db.OrderAttachments.Remove(attachment);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
    
}
