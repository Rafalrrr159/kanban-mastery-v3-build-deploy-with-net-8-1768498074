using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanApi.Models
{
    public class BoardMember
    {
        public string UserId { get; set; } = string.Empty;
        
        public int BoardId { get; set; }
        
        public BoardRole Role { get; set; } = BoardRole.Member;
        
        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [ForeignKey(nameof(BoardId))]
        public Board? Board { get; set; }
    }
}
