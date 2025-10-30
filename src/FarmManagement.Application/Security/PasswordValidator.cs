using System.Text.RegularExpressions;

namespace FarmManagement.Application.Security
{
    public static class PasswordValidator
    {
        public static bool IsValid(string password, out string? error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(password))
            {
                error = "Password cannot be empty.";
                return false;
            }

            if (password.Length < 8 || password.Length > 16)
            {
                error = "Password must be between 8 and 16 characters.";
                return false;
            }

            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                error = "Password must contain at least one uppercase letter.";
                return false;
            }

            if (!Regex.IsMatch(password, @"\d"))
            {
                error = "Password must contain at least one digit.";
                return false;
            }

            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?""{}|<>_\-\\[\];:]"))
            {
                error = "Password must contain at least one special character.";
                return false;
            }

            return true;
        }
    }
}
