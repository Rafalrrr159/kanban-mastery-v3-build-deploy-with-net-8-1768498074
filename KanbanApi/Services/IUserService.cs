namespace KanbanApi.Services
{
    public record UserProfileDto(string Id, string UserName, string Email);

    public interface IUserService
    {
        Task<UserProfileDto?> GetUserProfileAsync(string userId);
    }
}
