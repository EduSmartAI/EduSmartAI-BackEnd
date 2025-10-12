namespace AiService.Application.DTOs
{
	// Classes để bind JSON của model (khớp schema)
	public sealed class ModelJson
	{
		public string summary { get; set; } = "";
		public List<string>? strengths { get; set; }
		public List<string>? improvements { get; set; }
		public List<ActionJson>? actions { get; set; }
		public List<GapJson>? skill_gaps { get; set; }
		public int score100 { get; set; }
		public double confidence { get; set; }
	}
	public sealed class ActionJson { public string title { get; set; } = ""; public string kind { get; set; } = ""; public string? target_url { get; set; } }
	public sealed class GapJson { public string skill_tag { get; set; } = ""; public int level { get; set; } public string evidence { get; set; } = ""; }
}
