using KanbanApi.Models;

namespace KanbanApi.Services
{
    public interface ICardService
    {
        Task<Card?> CreateAsync(int boardId, CreateCardDto dto);
        Task<Card?> UpdateAsync(int boardId, int cardId, UpdateCardDto dto);
        Task<bool> DeleteAsync(int boardId, int cardId);
    }
}