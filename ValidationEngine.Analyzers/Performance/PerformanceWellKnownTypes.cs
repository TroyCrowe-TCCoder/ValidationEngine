namespace ValidationEngine.Analyzers.Performance
{
	/// <summary>
	/// Shared metadata names and constants reused across the Performance domain analyzers.
	/// Centralizing these avoids duplicating lookup/detection boilerplate in every individual
	/// GlobalPerformanceStandards.md rule analyzer.
	/// </summary>
	internal static class PerformanceWellKnownTypes
	{
		public const string CancellationTokenTypeName = "CancellationToken";
		public const string CancellationTokenMetadataName = "System.Threading.CancellationToken";

		public const string HttpClientTypeName = "HttpClient";
		public const string HttpClientMetadataName = "System.Net.Http.HttpClient";
		public const string TimeoutPropertyName = "Timeout";

		public const string TaskTypeName = "Task";
		public const string TaskMetadataName = "System.Threading.Tasks.Task";
		public const string ValueTaskTypeName = "ValueTask";
		public const string ValueTaskMetadataName = "System.Threading.Tasks.ValueTask";

		public const string ProgramFileName = "Program.cs";
	}
}