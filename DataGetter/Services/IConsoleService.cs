using DataGetter.Models;

namespace DataGetter.Services
{
    public interface IConsoleService
    {
        Article? GetArticle();
        Task SendArticleAsync();
    }
}