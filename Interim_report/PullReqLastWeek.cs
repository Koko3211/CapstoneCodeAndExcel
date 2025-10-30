using System.Reactive.Linq;
using Octokit;
using Octokit.Reactive;

class Program
{
    /*
    Count PRs created in the past year with weekly bins. I used this code for the graphs.
    I will not add comments as they are the same as "IssueReqLastWeek.cs"
    */
    private const string Owner  = "home-assistant";
    private const string Repo = "core";
    

    public static async Task Main()
    {
        var basicAuth = new Credentials("[TOKEN]");

        var Client = new GitHubClient(new ProductHeaderValue("Capstone"));
        Client.Credentials = basicAuth;


        var endDay = DateTime.UtcNow.Date.AddDays(-2); 
        for (int i = 0; i < 52; i++)
        {
            var startDay = endDay.AddDays(-7);
            var endInclusive = endDay.AddDays(-1);

            var query = $"repo:{Owner}/{Repo} is:pr created:{startDay:yyyy-MM-dd}..{endInclusive:yyyy-MM-dd}";
            var result = await Client.Search.SearchIssues(new SearchIssuesRequest(query));

            Console.WriteLine($"{startDay:yyyy-MM-dd} .. {endDay:yyyy-MM-dd}: {result.TotalCount}");

            endDay = startDay; 
        }
    }
}

