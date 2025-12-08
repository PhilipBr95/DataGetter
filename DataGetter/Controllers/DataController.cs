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
        public Article? Get()
        {
            var article = _consoleService.GetArticle();
            return article;
        }

        [HttpPut(Name = "SendArticleAsync")]
        public IActionResult SendArticleAsync()
        {
            _ = _consoleService.SendArticleAsync();
            return Ok();
        }
    }
}
