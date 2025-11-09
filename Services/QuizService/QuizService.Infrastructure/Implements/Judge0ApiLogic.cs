using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using QuizService.Application.Judge0Logics;
using QuizService.Application.Judge0Logics.Models;
using QuizService.Domain.WriteModels;

namespace QuizService.Infrastructure.Implements;

public class Judge0ApiLogic : IJudge0ApiLogic
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public Judge0ApiLogic()
    {
        EnvLoader.Load();
        _httpClient = new HttpClient();
        _baseUrl = Environment.GetEnvironmentVariable(ConstEnv.Judge0BaseUrl)!.TrimEnd('/');
        _httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Key", Environment.GetEnvironmentVariable(ConstEnv.Judge0ApiKey)!);
        _httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Host", "judge0-ce.p.rapidapi.com");
    }
    
    /// <summary>
    /// Submit single submission
    /// </summary>
    public async Task<SubmissionResponse> SubmitCodeAsync(SubmissionRequest request)
    {
        var encodedRequest = new SubmissionRequestEncoded
        {
            SourceCode = Convert.ToBase64String(Encoding.UTF8.GetBytes(request.SourceCode)),
            LanguageId = request.LanguageId,
            Stdin = Convert.ToBase64String(Encoding.UTF8.GetBytes(request.Stdin)),
            ExpectedOutput = Convert.ToBase64String(Encoding.UTF8.GetBytes(request.ExpectedOutput)),
            CpuTimeLimit = request.CpuTimeLimit,
            WallTimeLimit = request.WallTimeLimit,
            MemoryLimit = request.MemoryLimit
        };

        var json = JsonSerializer.Serialize(encodedRequest, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_baseUrl}/submissions?base64_encoded=true&wait=false", content);
        
        response.EnsureSuccessStatusCode();
        
        var responseBody = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SubmissionResponse>(responseBody, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower 
        })!;
    }

    /// <summary>
    /// Submit and wait for result
    /// </summary>
    public async Task<SubmissionResult> SubmitAndWaitAsync(SubmissionRequest request)
    {
        var encodedRequest = new SubmissionRequestEncoded
        {
            SourceCode = Convert.ToBase64String(Encoding.UTF8.GetBytes(request.SourceCode)),
            LanguageId = request.LanguageId,
            Stdin = Convert.ToBase64String(Encoding.UTF8.GetBytes(request.Stdin)),
            ExpectedOutput = Convert.ToBase64String(Encoding.UTF8.GetBytes(request.ExpectedOutput)),
            CpuTimeLimit = request.CpuTimeLimit,
            WallTimeLimit = request.WallTimeLimit,
            MemoryLimit = request.MemoryLimit
        };

        var json = JsonSerializer.Serialize(encodedRequest, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_baseUrl}/submissions?base64_encoded=true&wait=true", content);
        
        response.EnsureSuccessStatusCode();
        
        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SubmissionResultEncoded>(responseBody, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower 
        });

        return DecodeSubmissionResult(result);
    }

    /// <summary>
    /// Get submission result by token
    /// </summary>
    public async Task<SubmissionResult> GetSubmissionAsync(string token)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/submissions/{token}?base64_encoded=true");
        
        response.EnsureSuccessStatusCode();
        
        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SubmissionResultEncoded>(responseBody, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower 
        });

        return DecodeSubmissionResult(result);
    }

    /// <summary>
    /// Submit batch submissions
    /// </summary>
    public async Task<BatchSubmissionResponse> SubmitBatchAsync(BatchSubmissionRequest request)
    {
        var encodedRequest = new BatchSubmissionRequestEncoded
        {
            Submissions = request.Submissions.Select(s => new SubmissionRequestEncoded
            {
                SourceCode = Convert.ToBase64String(Encoding.UTF8.GetBytes(s.SourceCode)),
                LanguageId = s.LanguageId,
                Stdin = Convert.ToBase64String(Encoding.UTF8.GetBytes(s.Stdin)),
                ExpectedOutput = Convert.ToBase64String(Encoding.UTF8.GetBytes(s.ExpectedOutput)),
                CpuTimeLimit = s.CpuTimeLimit,
                WallTimeLimit = s.WallTimeLimit,
                MemoryLimit = s.MemoryLimit
            }).ToList()
        };

        var json = JsonSerializer.Serialize(encodedRequest, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_baseUrl}/submissions/batch?base64_encoded=true", content);
        
        response.EnsureSuccessStatusCode();
        
        var responseBody = await response.Content.ReadAsStringAsync();
        var jsonDeserialize = JsonSerializer.Deserialize<List<SubmissionResponse>>(responseBody, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower 
        })!;
        return new BatchSubmissionResponse
        {
            Submissions = jsonDeserialize
        };
    }

    /// <summary>
    /// Get batch submission results
    /// </summary>
    public async Task<List<SubmissionResult>> GetBatchSubmissionAsync(string tokens)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/submissions/batch?tokens={tokens}&base64_encoded=true");
        
        response.EnsureSuccessStatusCode();
        
        var responseBody = await response.Content.ReadAsStringAsync();
        var batchResult = JsonSerializer.Deserialize<BatchSubmissionResultEncoded>(responseBody, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower 
        });
        return batchResult!.Submissions.Select(DecodeSubmissionResult).ToList();

    }

    /// <summary>
    /// Get available languages
    /// </summary>
    public async Task<List<CodeLanguage>> GetLanguagesAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/languages");
        response.EnsureSuccessStatusCode();
        
        var responseBody = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<CodeLanguage>>(responseBody, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower 
        })!;
    }

    private SubmissionResult DecodeSubmissionResult(SubmissionResultEncoded encoded)
    {
        return new SubmissionResult
        {
            Token = encoded.Token,
            Status = encoded.Status,
            Stdout = DecodeBase64(encoded.Stdout),
            Stderr = DecodeBase64(encoded.Stderr),
            CompileOutput = DecodeBase64(encoded.CompileOutput),
            Message = DecodeBase64(encoded.Message),
            Time = Double.Parse(encoded.Time),
            Memory = encoded.Memory
        };
    }

    private string DecodeBase64(string base64String)
    {
        try
        {
            var bytes = Convert.FromBase64String(base64String);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return base64String;
        }
    }
}

internal class SubmissionRequestEncoded
{
    [JsonPropertyName("source_code")]
    public string SourceCode { get; set; }
        
    [JsonPropertyName("language_id")]
    public int LanguageId { get; set; }
        
    [JsonPropertyName("stdin")]
    public string Stdin { get; set; }
        
    [JsonPropertyName("expected_output")]
    public string ExpectedOutput { get; set; }
        
    [JsonPropertyName("cpu_time_limit")]
    public double? CpuTimeLimit { get; set; }
        
    [JsonPropertyName("wall_time_limit")]
    public double? WallTimeLimit { get; set; }
        
    [JsonPropertyName("memory_limit")]
    public int? MemoryLimit { get; set; }
}

internal class BatchSubmissionRequestEncoded
{
    [JsonPropertyName("submissions")]
    public List<SubmissionRequestEncoded> Submissions { get; set; }
}

internal class SubmissionResultEncoded
{
    [JsonPropertyName("token")]
    public string Token { get; set; }
        
    [JsonPropertyName("status")]
    public Status Status { get; set; }
        
    [JsonPropertyName("stdout")]
    public string Stdout { get; set; }
        
    [JsonPropertyName("stderr")]
    public string Stderr { get; set; }
        
    [JsonPropertyName("compile_output")]
    public string CompileOutput { get; set; }
        
    [JsonPropertyName("message")]
    public string Message { get; set; }
        
    [JsonPropertyName("time")]
    public string Time { get; set; }
        
    [JsonPropertyName("memory")]
    public int? Memory { get; set; }
}

internal class BatchSubmissionResultEncoded
{
    [JsonPropertyName("submissions")]
    public List<SubmissionResultEncoded> Submissions { get; set; }
}
