namespace StokvelSync.Client.Services;
public class UserSession {
    public string? Email { get; set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(Email);
}