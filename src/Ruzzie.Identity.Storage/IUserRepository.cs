using System;

namespace Ruzzie.Identity.Storage;

public interface IUserRepository<TUser>
{
    bool   UserExists(string     userId);
    TUser  InsertNewUser(TUser   userEntity);
    TUser? GetUserByEmail(string userId);
    TUser  UpdateUser(TUser      userEntity, DateTimeOffset? utcNow = default);
    void   DeleteUser(string     userId);
}