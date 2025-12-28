using BuildingBlocks.CQRS;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StudentService.Application.Applications.Students.Commands.Updates;

public class StudentProfileUpdateCommand : ICommand<StudentProfileUpdateResponse>
{
    [StringLength(50, ErrorMessage = "Họ không được vượt quá 50 ký tự.")]
    public string? FirstName { get; set; }

    [StringLength(50, ErrorMessage = "Tên không được vượt quá 50 ký tự.")]
    public string? LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự.")]
    public string? PhoneNumber { get; set; }

    [Range(1, 3, ErrorMessage = "Giới tính không hợp lệ. (1: Nam, 2: Nữ, 3: Khác)")]
    public short? Gender { get; set; }

    public IFormFile? Avatar { get; set; }

    [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự.")]
    public string? Address { get; set; }
    
    [StringLength(500, ErrorMessage = "Giới thiệu bản thân không được vượt quá 500 ký tự.")]
    public string? Bio { get; set; }
    
    public Guid? MajorId { get; set; }

    public Guid? SemesterId { get; set; }

    [MinLength(1, ErrorMessage = "Phải chọn ít nhất một công nghệ.")]
    public List<Guid>? Technologies { get; set; }

    [MinLength(1, ErrorMessage = "Phải chọn ít nhất một mục tiêu học tập.")]
    public List<Guid>? LearningGoals { get; set; }
}