using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebCodeFlowPkceClient.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IHttpClientFactory httpClientFactory, 
        ILogger<IndexModel> logger)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task OnGetAsync()
    {
        var claims = User.Claims.ToList();

        var client = _httpClientFactory.CreateClient("dpop-api-client");

        var response = await client.GetStringAsync("api/values");
    }
}