using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace AuthService.Application.Accounts.Commands.Inserts;

public record StudentInsertCommand : ICommand<StudentInsertResponse>
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    [DefaultValue("edusmartAI@gmail.com")]
    public string Email { get; init; } = null!;
    
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6-100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$",
        ErrorMessage = "Password must contain uppercase, lowercase, number and special character")]    [DefaultValue("Edusmart@123")]
    public string Password { get; init; } = null!;
    
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2-50 characters")]
    [RegularExpression(@"^[\p{L}\s]+$", ErrorMessage = "First name can only contain letters (including Unicode) and spaces")]
    [DefaultValue("Edu")]
    public string FirstName { get; init; } = null!;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2-50 characters")]
    [RegularExpression(@"^[\p{L}\s]+$", ErrorMessage = "Last name can only contain letters (including Unicode) and spaces")]
    [DefaultValue("Smárt")]
    public string LastName { get; init; } = null!;}