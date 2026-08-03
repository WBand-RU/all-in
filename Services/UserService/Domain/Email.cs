using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using FluentValidation.Validators;

namespace UserService.Domain;

public readonly struct Email
{
    public readonly string Value;

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentNullException(nameof(value));
        }

        _ = new MailAddress(value);

        return new Email(value);
    }

    private Email(string value)
    {
        Value = value;
    }
}
