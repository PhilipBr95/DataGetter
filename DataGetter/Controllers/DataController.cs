using DataGetter.Models;
using DataGetter.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DataGetter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DataController : ControllerBase
    {
        private readonly IConsoleService _consoleService;
        private readonly ILogger<DataController> _logger;

        public DataController(IConsoleService consoleService, ILogger<DataController> logger)
        {
            _consoleService = consoleService;
            _logger = logger;
        }

        [HttpGet(Name = "GetArticle")]
        public async Task<Article?> Get()
        {
            var article = await _consoleService.GetArticleAsync();
            return article;
        }

        [HttpPut(Name = "SendArticleAsync")]
        public async Task<IActionResult> SendArticleAsync()
        {
            await _consoleService.SendArticleAsync();
            return Ok();
        }
    }
}
