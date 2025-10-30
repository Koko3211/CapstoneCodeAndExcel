using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Octokit;
using Octokit.Reactive;

class Program
{
    private const string Owner  = "home-assistant";
    private const string Repo = "core";
    /*
    Count issues created in the past year with weekly bins. I used this code for the graphs.
    I added comments to each part of the code to explain what it does
    */

    public static async Task Main()
    {
        //Client setup
        var basicAuth = new Credentials("[TOKEN]");
        var Client = new GitHubClient(new ProductHeaderValue("Capstone"));
        Client.Credentials = basicAuth;

        //Subtracted 2 days to have the same time frame as the other graphs
        //I created this code 2 days later than the "commits last year"
        var endDay = DateTime.UtcNow.Date.AddDays(-2); 
        //Loop over 52 weeks
        for (int i = 0; i < 52; i++)
        {
            //Set up time window
            var startDay = endDay.AddDays(-7);
            var endInclusive = endDay.AddDays(-1);
            //Create search query for issues created in the time window
            var query = $"repo:{Owner}/{Repo} is:issue type:issue created:{startDay:yyyy-MM-dd}..{endInclusive:yyyy-MM-dd}";
            //Runs the search query
            var result = await Client.Search.SearchIssues(new SearchIssuesRequest(query));

            Console.WriteLine($"{startDay:yyyy-MM-dd} .. {endDay:yyyy-MM-dd}: {result.TotalCount}");
            //Move the time window back by one week
            endDay = startDay; 
        }
    }
}

