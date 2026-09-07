using DataGetter.Models;

namespace DataGetter.Services
{
    public interface IConsoleService
    {
        Task<Article?> GetArticleAsync();
        Task SendArticleAsync();
    }
}