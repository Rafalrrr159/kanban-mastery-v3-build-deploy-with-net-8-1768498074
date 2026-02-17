using System.ComponentModel.DataAnnotations;

namespace KanbanApi.Models
{
    public class Board
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        public ICollection<Column> Columns { get; set; } = new List<Column>();
        
        public ICollection<BoardMember> Members { get; set; } = new List<BoardMember>();
    }
}
