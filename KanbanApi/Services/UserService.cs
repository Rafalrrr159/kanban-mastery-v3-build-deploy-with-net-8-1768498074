using KanbanApi.Data;

namespace KanbanApi.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _db;

        public UserService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<UserProfileDto?> GetUserProfileAsync(string userId)
        {
            var appUser = await _db.Users.FindAsync(userId);

            if (appUser is null) return null;

            return new UserProfileDto(appUser.Id, appUser.UserName, appUser.Email);
        }
    }
}
