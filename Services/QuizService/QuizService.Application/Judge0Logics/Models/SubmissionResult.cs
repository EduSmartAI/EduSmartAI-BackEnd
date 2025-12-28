using System.Text.Json.Serialization;

namespace QuizService.Application.Judge0Logics.Models;

public class SubmissionResult
{
    public string Token { get; set; }
    public Status Status { get; set; }
    public string Stdout { get; set; }
    public string Stderr { get; set; }
    public string CompileOutput { get; set; }
    public string Message { get; set; }
    public double? Time { get; set; }
    public int? Memory { get; set; }
}

public class Status
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
}