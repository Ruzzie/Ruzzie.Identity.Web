using System;

namespace Ruzzie.Identity.Storage;

public interface IUserOrgProps
{
    string?        Role                      { get; set; }
    DateTimeOffset JoinedCreationDateTimeUtc { get; set; }
}