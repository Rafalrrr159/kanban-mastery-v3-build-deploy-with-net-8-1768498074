using KanbanApi.Data;
using KanbanApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KanbanApi.Services;

public class BoardService : IBoardService
{
    private readonly ApplicationDbContext _context;

    public BoardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Board>> GetByUserIdAsync(string userId)
    {
        return await _context.Boards
            .AsNoTracking()
            .Where(b => b.Members.Any(m => m.UserId == userId))
            .ToListAsync();
    }

    public async Task<Board?> GetByIdAsync(int id)
    {
        return await _context.Boards
            .Include(b => b.Columns)
                .ThenInclude(c => c.Cards)
            .Include(b => b.Members)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Board> CreateAsync(Board board, string ownerUserId)
    {
        var ownerMember = new BoardMember
        {
            Board = board,
            UserId = ownerUserId,
            Role = BoardRole.Owner
        };

        _context.Boards.Add(board);
        _context.BoardMembers.Add(ownerMember);

        await _context.SaveChangesAsync();

        return board;
    }

    public async Task<Board?> UpdateAsync(int id, Board board)
    {
        var existingBoard = await _context.Boards.FindAsync(id);
        if (existingBoard is null)
        {
            return null;
        }

        existingBoard.Name = board.Name;

        await _context.SaveChangesAsync();

        return existingBoard;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var board = await _context.Boards.FindAsync(id);
        if (board is null)
        {
            return false;
        }

        _context.Boards.Remove(board);

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public async Task<bool> AddMemberAsync(int boardId, string userId)
    {
        var exists = await _context.BoardMembers.AnyAsync(m => m.BoardId == boardId && m.UserId == userId);
        if (exists) return false;

        var membership = new BoardMember
        {
            BoardId = boardId,
            UserId = userId,
            Role = BoardRole.Member
        };

        _context.BoardMembers.Add(membership);
        await _context.SaveChangesAsync();
        return true;
    }
}

