using System.Reflection;
using System.Text.RegularExpressions;

namespace BaseService.Common.Utils.Const;

public partial class Messages
{
    private static string? _csvContent;
    
    /// <summary>
    /// Returns the message associated with the message ID.
    /// </summary>
    /// <param name="messageId">messageId</param>
    /// <param name="args">args</param>
    public static string GetMessage(string messageId, params string[] args)
    {
        var message = "No matching message.";
        
        try
        {
            // Load CSV content from embedded resource
            if (_csvContent == null)
            {
                _csvContent = LoadCsvFromEmbeddedResource();
            }
            
            if (string.IsNullOrEmpty(_csvContent))
            {
                return "MessageId.csv not found in embedded resources.";
            }
            
            var lines = _csvContent.Split('\n');
            
            // Skip header row
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                string[] values = line.Split(',');
                if (values.Length >= 3 && values[1] == messageId)
                {
                    message = values[2];
                    
                    // Replace placeholders with arguments
                    for (int j = 0; j < args.Length; j++)
                        message = message.Replace($"{{{j}}}", args[j]);
                    
                    // Remove remaining placeholders
                    message = Regex.Replace(message, @"\{[0-9]+\}", "");
                    
                    // Convert \\n to \n
                    message = message.Replace("\\n", "\n");
                    
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            return $"Error reading message: {ex.Message}";
        }
        
        return message;
    }
    
    private static string LoadCsvFromEmbeddedResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "BaseService.Common.Settings.ConstanstCSV.MessageId.csv";
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return string.Empty;
        }
        
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}