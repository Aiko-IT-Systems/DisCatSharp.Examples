using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

using System.Diagnostics;

namespace DisCatSharp.Examples.Hosting.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true), IgnoreAntiforgeryToken]
public class ErrorModel(ILogger<ErrorModel> logger) : PageModel
{
	public string RequestId { get; set; }

	public bool ShowRequestId => !string.IsNullOrEmpty(this.RequestId);

	private readonly ILogger<ErrorModel> _logger = logger;

	public void OnGet() => this.RequestId = Activity.Current?.Id ?? this.HttpContext.TraceIdentifier;
}
