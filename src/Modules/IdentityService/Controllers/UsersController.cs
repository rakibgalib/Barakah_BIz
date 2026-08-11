using System.Security.Claims;
using Barakah.IdentityService.Data;
using Barakah.IdentityService.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Barakah.IdentityService.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(IdentityDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userId is null || !Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(new UserResponse(user.Id, user.Email, user.FirstName, user.LastName, user.TenantId, user.EmailVerified));
    }
}
