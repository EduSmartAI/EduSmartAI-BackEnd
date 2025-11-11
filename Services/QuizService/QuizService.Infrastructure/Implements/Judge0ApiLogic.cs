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
            Stdin = !string.IsNullOrEmpty(request.Stdin) 
                ? Convert.ToBase64String(Encoding.UTF8.GetBytes(request.Stdin)) 
                : null,
            ExpectedOutput = !string.IsNullOrEmpty(request.ExpectedOutput) 
                ? Convert.ToBase64String(Encoding.UTF8.GetBytes(request.ExpectedOutput)) 
                : null,
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
            Stdin = !string.IsNullOrEmpty(request.Stdin) 
                ? Convert.ToBase64String(Encoding.UTF8.GetBytes(request.Stdin)) 
                : null,
            ExpectedOutput = !string.IsNullOrEmpty(request.ExpectedOutput) 
                ? Convert.ToBase64String(Encoding.UTF8.GetBytes(request.ExpectedOutput)) 
                : null,
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
                Stdin = !string.IsNullOrEmpty(s.Stdin) 
                    ? Convert.ToBase64String(Encoding.UTF8.GetBytes(s.Stdin)) 
                    : null,
                ExpectedOutput = !string.IsNullOrEmpty(s.ExpectedOutput) 
                    ? Convert.ToBase64String(Encoding.UTF8.GetBytes(s.ExpectedOutput)) 
                    : null,
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
    /// Get available languages with detailed information
    /// </summary>
    public async Task<List<CodeLanguage>> GetLanguagesAsync()
    {
        // STEP 1: Get list of all languages (only id and name)
        var response = await _httpClient.GetAsync($"{_baseUrl}/languages");
        response.EnsureSuccessStatusCode();
        
        var responseBody = await response.Content.ReadAsStringAsync();
        var languages = JsonSerializer.Deserialize<List<LanguageBasicInfo>>(responseBody, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower 
        })!;
        
        // STEP 2: Get detailed information for each language
        var detailedLanguages = new List<CodeLanguage>();
        
        foreach (var lang in languages)
        {
            try
            {
                var detailResponse = await _httpClient.GetAsync($"{_baseUrl}/languages/{lang.Id}");
                detailResponse.EnsureSuccessStatusCode();
                
                var detailBody = await detailResponse.Content.ReadAsStringAsync();
                var judge0Lang = JsonSerializer.Deserialize<Judge0LanguageDetail>(detailBody, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower 
                });
                
                if (judge0Lang != null)
                {
                    // Convert Judge0LanguageDetail to CodeLanguage entity
                    var codeLanguage = new CodeLanguage
                    {
                        LanguageId = judge0Lang.Id,
                        Name = judge0Lang.Name,
                        IsArchived = judge0Lang.IsArchived,
                        SourceFile = judge0Lang.SourceFile ?? "",
                        CompileCmd = judge0Lang.CompileCmd ?? "",
                        RunCmd = judge0Lang.RunCmd ?? "",
                        IsActive = true
                    };
                    
                    detailedLanguages.Add(codeLanguage);
                }
            }
            catch (Exception ex)
            {
                // Log error but continue with other languages
                Console.WriteLine($"Failed to get details for language {lang.Id}: {ex.Message}");
            }
        }
        
        return detailedLanguages;
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
            Time = ParseNullableDouble(encoded.Time),
            Memory = encoded.Memory
        };
    }
    
    private double? ParseNullableDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (double.TryParse(value, out double result))
            return result;

        return null;
    }

    private string? DecodeBase64(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            return null;

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }
        catch
        {
            return base64;
        }
    }
}

internal class SubmissionRequestEncoded
{
    [JsonPropertyName("source_code")]
    public string SourceCode { get; set; } = null!;
        
    [JsonPropertyName("language_id")]
    public int LanguageId { get; set; }
        
    [JsonPropertyName("stdin")]
    public string? Stdin { get; set; }
        
    [JsonPropertyName("expected_output")]
    public string? ExpectedOutput { get; set; }
        
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

internal class LanguageBasicInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

internal class Judge0LanguageDetail
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("is_archived")]
    public bool IsArchived { get; set; }
    
    [JsonPropertyName("source_file")]
    public string SourceFile { get; set; }
    
    [JsonPropertyName("compile_cmd")]
    public string CompileCmd { get; set; }
    
    [JsonPropertyName("run_cmd")]
    public string RunCmd { get; set; }
}

