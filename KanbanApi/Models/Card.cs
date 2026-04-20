using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanApi.Models
{
    public class Card
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Title { get; set; }

        public string? Description { get; set; }

        public int ColumnId { get; set; }

        [ForeignKey(nameof(ColumnId))]
        public Column? Column { get; set; }

        public string? AssignedToUserId { get; set; }

        [ForeignKey(nameof(AssignedToUserId))]
        public ApplicationUser? AssignedToUser { get; set; }
    }
}
