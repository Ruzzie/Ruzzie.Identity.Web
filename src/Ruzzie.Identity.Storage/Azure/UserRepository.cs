using System;
using Ruzzie.Azure.Storage;
using Ruzzie.Identity.Storage.Azure.Entities;

namespace Ruzzie.Identity.Storage.Azure;

public class UserRepository : IUserRepository
{
    private readonly CloudTablePool _userRegistrationTable;

    public UserRepository(CloudTablePool userRegistrationTable)
    {
        _userRegistrationTable = userRegistrationTable;
    }

    public bool UserExists(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(email));
        }

        var partitionKey = string.Empty;

        try
        {
            partitionKey = email.CreateAlphaNumericPartitionKey().ToString();
            return _userRegistrationTable.RowExistsForPartitionKeyAndRowKey(partitionKey, email);
        }
        catch (Exception e)
        {
            throw new Exception($"Error checking UserExists in [{_userRegistrationTable.TableName}] with [{email}]: [{partitionKey}]-[{email}]. [{e.Message}]", e);
        }
    }

    public UserRegistration InsertNewUser(UserRegistration userEntity)
    {
        if (ReferenceEquals(userEntity, null))
        {
            throw new ArgumentNullException(nameof(userEntity));
        }

        try
        {
            return _userRegistrationTable.InsertEntity(userEntity);
        }
        catch (Exception e)
        {
            throw new Exception(
                                $"Error for [{nameof(InsertNewUser)}] in [{_userRegistrationTable.TableName}]: [{userEntity.PartitionKey}]-[{userEntity.RowKey}]. [{e.Message}]",
                                e);
        }
    }

    public UserRegistration GetUserByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(email));
        }

        var partitionKey = string.Empty;

        try
        {
            partitionKey = email.CreateAlphaNumericPartitionKey().ToString();
            return _userRegistrationTable.GetEntity<UserRegistration>(partitionKey, email);
        }
        catch (Exception e)
        {
            throw new Exception($"Error for [{nameof(GetUserByEmail)}] in [{_userRegistrationTable.TableName}] with [{email}]: [{partitionKey}]-[{email}]. [{e.Message}]", e);
        }
    }

    public UserRegistration UpdateUser(UserRegistration userEntity, DateTimeOffset? utcNow = default)
    {
        if (ReferenceEquals(userEntity, null))
        {
            throw new ArgumentNullException(nameof(userEntity));
        }

        try
        {
            userEntity.LastModifiedDateTimeUtc = utcNow ?? DateTimeOffset.UtcNow;
            return _userRegistrationTable.UpdateEntity(userEntity);
        }
        catch (Exception e)
        {
            throw new Exception(
                                $"Error for [{nameof(UpdateUser)}] in [{_userRegistrationTable.TableName}]: [{userEntity.PartitionKey}]-[{userEntity.RowKey}]. [{e.Message}]",
                                e);
        }
    }

    public void DeleteUser(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(email));
        }

        var partitionKey = string.Empty;

        try
        {
            partitionKey = email.CreateAlphaNumericPartitionKey().ToString();
            _userRegistrationTable.Delete(partitionKey, email);
        }
        catch (Exception e)
        {
            throw new Exception($"Error for [{nameof(DeleteUser)}] in [{_userRegistrationTable.TableName}] with [{email}]: [{partitionKey}]-[{email}]. [{e.Message}]", e);
        }
    }
}