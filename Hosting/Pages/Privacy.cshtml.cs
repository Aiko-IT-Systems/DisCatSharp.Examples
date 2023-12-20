using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace DisCatSharp.Examples.Hosting.Pages;

public class PrivacyModel(ILogger<PrivacyModel> logger) : PageModel
{
	private readonly ILogger<PrivacyModel> _logger = logger;

	public void OnGet()
	{ }
}
