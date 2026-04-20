namespace Fortuno.DTO.NAuth;

public class NAuthUserInfo
{
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DocumentId { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}
