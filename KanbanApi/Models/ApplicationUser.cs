using Microsoft.AspNetCore.Identity;

namespace KanbanApi.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<BoardMember> BoardMemberships { get; set; } = new List<BoardMember>();
    }
}
