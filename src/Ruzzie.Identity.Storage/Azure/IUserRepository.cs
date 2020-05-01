using Ruzzie.Identity.Storage.Azure.Entities;

namespace Ruzzie.Identity.Storage.Azure
{
    public interface IUserRepository : IUserRepository<UserRegistration>
    {
        // bool UserExists(string email);
        // UserRegistration InsertNewUser(UserRegistration userEntity);
        // UserRegistration GetUserByEmail(string email);
        // UserRegistration UpdateUser(UserRegistration userEntity, DateTimeOffset? utcNow = default);
        // void DeleteUser(string email);
    }
}