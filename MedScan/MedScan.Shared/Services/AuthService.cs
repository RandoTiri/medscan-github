using MedScan.Shared.Models;

namespace MedScan.Shared.Services;

public class AuthService
{
    private AppUser? _registeredUser;

    public bool IsLoggedIn { get; private set; }
    public AppUser? CurrentUser { get; private set; }

    public bool HasRegisteredUser()
    {
        return _registeredUser is not null;
    }

    public bool Register(string fullName, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        if (_registeredUser is not null)
        {
            return false;
        }

        _registeredUser = new AppUser
        {
            FullName = fullName,
            Email = email,
            Password = password
        };

        CurrentUser = _registeredUser;
        IsLoggedIn = true;
        return true;
    }

    public bool Login(string email, string password)
    {
        if (_registeredUser is null)
            return false;

        if (_registeredUser.Email == email && _registeredUser.Password == password)
        {
            CurrentUser = _registeredUser;
            IsLoggedIn = true;
            return true;
        }

        return false;
    }

    public void Logout()
    {
        CurrentUser = null;
        IsLoggedIn = false;
    }
}