using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace DisCatSharp.Examples.Hosting.Pages;

public class IndexModel(ILogger<IndexModel> logger) : PageModel
{
	private readonly ILogger<IndexModel> _logger = logger;

	public void OnGet()
	{ }
}
