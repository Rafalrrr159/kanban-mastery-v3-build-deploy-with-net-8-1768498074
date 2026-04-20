using KanbanApi.Data;
using KanbanApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KanbanApi.Authorization
{
    public class IsBoardOwnerRequirement : IAuthorizationRequirement { }

    public class IsBoardOwnerHandler : AuthorizationHandler<IsBoardOwnerRequirement, int>
    {
        private readonly ApplicationDbContext _db;

        public IsBoardOwnerHandler(ApplicationDbContext db)
        {
            _db = db;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            IsBoardOwnerRequirement requirement,
            int boardId)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return;

            var isOwner = await _db.BoardMembers
                .AnyAsync(m => m.BoardId == boardId && m.UserId == userId && m.Role == BoardRole.Owner);

            if (isOwner)
            {
                context.Succeed(requirement);
            }
        }
    }
}
