using Microsoft.AspNetCore.Identity;

namespace ClinicManagementSystem.Models
{
    // LABEL: Localized Identity Error Describer
    public class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
    {
        // LABEL: Default Error
        public override IdentityError DefaultError()
        {
            return new IdentityError
            {
                Code = nameof(DefaultError),
                Description = "An unknown failure has occurred."
            };
        }

        // LABEL: Password Too Short
        public override IdentityError PasswordTooShort(int length)
        {
            return new IdentityError
            {
                Code = nameof(PasswordTooShort),
                Description = $"Passwords must be at least {length} characters."
            };
        }

        // LABEL: Password Requires Unique Characters
        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresUniqueChars),
                Description = $"Passwords must use at least {uniqueChars} different characters."
            };
        }

        // LABEL: Password Requires Non-Alphanumeric
        public override IdentityError PasswordRequiresNonAlphanumeric()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresNonAlphanumeric),
                Description = "Passwords must have at least one non-alphanumeric character."
            };
        }

        // LABEL: Password Requires Digit
        public override IdentityError PasswordRequiresDigit()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresDigit),
                Description = "Passwords must have at least one digit ('0'-'9')."
            };
        }

        // LABEL: Password Requires Lowercase
        public override IdentityError PasswordRequiresLower()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresLower),
                Description = "Passwords must have at least one lowercase ('a'-'z')."
            };
        }

        // LABEL: Password Requires Uppercase
        public override IdentityError PasswordRequiresUpper()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresUpper),
                Description = "Passwords must have at least one uppercase ('A'-'Z')."
            };
        }

        // LABEL: Invalid Email
        public override IdentityError InvalidEmail(string email)
        {
            return new IdentityError
            {
                Code = nameof(InvalidEmail),
                Description = $"Email '{email}' is invalid."
            };
        }

        // LABEL: Duplicate Email
        public override IdentityError DuplicateEmail(string email)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateEmail),
                Description = $"Email '{email}' is already taken."
            };
        }

        // LABEL: Duplicate Username
        public override IdentityError DuplicateUserName(string userName)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateUserName),
                Description = $"Username '{userName}' is already taken."
            };
        }
    }
}