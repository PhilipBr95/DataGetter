using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataGetter
{
    internal class Settings
    {
        public string[] Urls =
        [
            "https://feeds.bbci.co.uk/news/rss.xml?edition=uk"
        ];

        public int ChangeArticleEverySeconds = 10;
        public int RefreshArticlesEveryCycle = 60;
    }
}
