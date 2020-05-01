#Tables for User Registration, Authentication and Authorization

- ! : mandatory field
- ? : optional field
- PartitionKey's and RowKey's are always mandatory and cannot be mutated, these are the immutable Id of a record.
- All string typed fields are limited by 32k bytes sizes;
- When records of a table must be Enumerable or a point query needs to be done for a single value (rowKey) the PartitionKey is always a calculable derivative of the RowKey, where the RowKey must be unique in the whole dataSet
For example: We could calculate the FNV Hash of the RowKey (string) and do it MOD 256 => Now we know we always have a maximum of 256 partitions; We can batch load the table by loading all the records of each partition (0-255)

## UserRegistrationTable
|      PartitionKey        |   RowKey  |   Email   |   Password   | Firstname | Lastname  | CreationDateTimeUtc | LastModifiedDateTimeUtc | AccountValidationStatus | EmailValidationToken | ValidationStatusUpdateDateTimeUtc | PasswordResetToken | PasswordResetTokenUpdateDateTimeUtc |
|--------------------------|-----------|-----------|--------------|-----------|-----------|---------------------|-------------------------|-------------------------|----------------------|-----------------------------------|--------------------|-------------------------------------|
| `(0-9 - A-Z) [Email][0]` | `[Email]` | !`string` | !`string`    | !`string` | !`string` |  !`DateTimeOffset`  |  ?`DateTimeOffset`      |        !`int32`         |  ?`string`           |        ?`DateTimeOffset`          |  ?`string`         |         ?`DateTimeOffset`           |

#### AccountValidationStatus
[See ApiTypes](https://github.com/PVH-Mobile-Automation/PVH-CAT-BACKEND/blob/master/Source/LinkBiServer/Pvh.Link.Bi.WebApi/ApiTypes.fs)

    type AccountValidationStatus =
        | None = 0
        | Validated = 1
        | Pending = 2

### Possible Queries
- Point Query By given Email + ?filters
- Enumerate all UserRegistrations: GetAllRowsPerPartition(0-9 - A-Z) : 
    - For Cleanup purposes (remove all unactivated accounts older than x time);
    - For Stats: How many activated accounts do we have; How many new accounts since x date
    
## OrganisationTable (alias CompanyTable)
|      PartitionKey       |               RowKey                      |   OrganisationName   |          CreatedByUserId           | CreationDateTimeUtc | LastModifiedDateTimeUtc |
|-------------------------|-------------------------------------------|----------------------|------------------------------------|---------------------|-------------------------|
|`(0-9 - A-Z) [RowKey][0]`|`(0-9 - A-Z)(UpperCase) [OrganisationName]`|     !`string`        | !`[UserRegistrationTable].RowKey`  |  !`DateTimeOffset`  |  ?`DateTimeOffset`      |

### Queries
 - Get All Organisations / Enumerate all companies

### UserOrganisationTable
|             PartitionKey       |            RowKey            |    Role    | JoinedCreationDateTimeUtc |
|--------------------------------|------------------------------|------------|---------------------------|
|`[UserRegistrationTable].RowKey`| `[OrganisationTable].RowKey` | !`string`  |      !`DateTimeOffset`    |

#### Possible Queries
- Get all organisations a user belongs to: Partition Scan: partitionKey = user.RowKey
- Get User Role for organisation

### OrganisationUserTable
|           PartitionKey     |               RowKey             |    Role    | JoinedCreationDateTimeUtc |
|----------------------------|----------------------------------|------------|---------------------------|
|`[OrganisationTable].RowKey`| `[UserRegistrationTable].RowKey` | !`string`  |      !`DateTimeOffset`    |

#### Possible Queries
- Get all users for an organisation: Partition Scan: partitionKey = organisation.RowKey
- Get Admin Role user for organisation

## OrganisationInvitesTable
|           PartitionKey      |  RowKey    | OrganisationId | InviteeEmail |          InvitedByUserId           | InvitationToken  |  InvitedAtDateTimeUtc | InvitationStatus | InvitationStatusUpdateDateTimeUtc |
|-----------------------------|------------|----------------|--------------|------------------------------------|------------------|-----------------------|------------------|-----------------------------------|
|`[OrganisationTable].RowKey` | `[Email]`  |  !`string`     |  !`string`   | !`[UserRegistrationTable].RowKey`  |     !string      |  !`DateTimeOffset`    |     !`int`       |  ?`DateTimeOffset`                |

### Possible Queries
- Get all Pending invitations for an organisation: PartitionScan: partitionKey = organisation.RowKey
- Get / Update invitation: Point Query: OrganisationId & Email
- 