using System;
using System.ComponentModel.DataAnnotations;
using Ruzzie.Common.Validation;

namespace Ruzzie.Extensions.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class EmailAddressV2Attribute : DataTypeAttribute
{
    public EmailAddressV2Attribute(): base(DataType.EmailAddress)
    {

    }

    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return true;
        }

        if (!(value is string valueAsString))
        {
            return false;
        }

        return valueAsString.IsValidEmailAddress();
    }
}