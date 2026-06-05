using System.ComponentModel.DataAnnotations;

namespace GLMS.Api.Models;

public class Client
{
    public int ClientId { get; set; }
    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;
    [Required, StringLength(30)]
    public string Phone { get; set; } = string.Empty;
    [Required, StringLength(80)]
    public string Region { get; set; } = string.Empty;

    public List<Contract> Contracts { get; set; } = [];
}
