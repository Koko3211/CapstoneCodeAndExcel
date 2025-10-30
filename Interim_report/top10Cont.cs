using System.Reactive.Linq;
using Octokit;
using Octokit.Reactive;

class Program
{
    /*
    Count top 10 contributors in the last year for commits, PRs and Issues
    */
    private const string Owner  = "home-assistant";
    private const string Repo   = "core";
    

    public static async Task Main()
    {
        //client set up
        var basicAuth = new Credentials("[TOKEN]");

        var Client = new GitHubClient(new ProductHeaderValue("Capstone"));
        Client.Credentials = basicAuth;
        var rx = new ObservableGitHubClient(Client);

        //Set time frame to the past year
        var since = DateTimeOffset.UtcNow.AddYears(-1);

        //Create requests
        var reqcom = new CommitRequest { Since = since, Sha = "dev" };

        var prRequest = new PullRequestRequest
        {
            State = ItemStateFilter.All,
            SortDirection = SortDirection.Descending,
        };
        var IssueRequest = new RepositoryIssueRequest
        {
            State = ItemStateFilter.All,
            SortDirection = SortDirection.Descending,
        };
        //Run queries and store information
        var prList = await rx.PullRequest
            .GetAllForRepository(Owner, Repo, prRequest)
            .Where(pr => pr.CreatedAt >= since)
            .ToList();

        var IssueList = await rx.Issue
            .GetAllForRepository(Owner, Repo, IssueRequest)
            .Where(i => i.PullRequest == null && i.CreatedAt >= since)
            .ToList();

        var countCommit = await rx.Repository.Commit
            .GetAll(Owner, Repo, reqcom)
            .Where(c => c.Commit?.Author?.Date >= since)
            .ToList();
    
        //Count contributions by contributor
        var CommByCont = countCommit
                    .GroupBy(c => c.Author?.Login ?? "Unknown")
                    .Select(c => new { Contributor = c.Key, CommitCount = c.Count() })
                    .OrderByDescending(x => x.CommitCount)
                    .ToList();

        var PRByCont = prList
                .GroupBy(p => p.User?.Login ?? "Unknown")
                .Select(p => new { Contributor = p.Key, countPR = p.Count() })
                .OrderByDescending(x => x.countPR)
                .ToList();
        var IssueByCont = IssueList
                .GroupBy(i => i.User?.Login ?? "Unknown")
                .Select(i => new { Contributor = i.Key, countIssues = i.Count() })
                .OrderByDescending(x => x.countIssues)
                .ToList();

        //Print top 10 contributors for each type
        Console.WriteLine("Comiters");
        for (int i = 0; i < 10; i++)
        {
            var contributor = CommByCont[i];
            Console.WriteLine($"{i + 1}. {contributor.Contributor}: {contributor.CommitCount} commits");
        }

        Console.WriteLine("PR");
        for (int i = 0; i < 10; i++)
        {
            var contributor = PRByCont[i];
            Console.WriteLine($"{i + 1}. {contributor.Contributor}: {contributor.countPR} PRs");
        } 

        Console.WriteLine("ISSUES");
        for (int i = 0; i < 10; i++)
        
        {
            var contributor = IssueByCont[i];
            Console.WriteLine($"{i + 1}. {contributor.Contributor}: {contributor.countIssues} Issu");
        }
    }
}

