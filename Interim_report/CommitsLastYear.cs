using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Octokit;
using Octokit.Reactive;

class Program
{
    /*
    Count commits in the last year
    I added comments to each part of the code to explain what it does
    */
    private const string Owner  = "home-assistant";
    private const string Repo   = "core";
    private const string? Branch = null; // or leave null to auto-resolve default

    public static async Task Main()
    {
        //Client setup
        var basicAuth = new Credentials("[TOKEN]");

        var Client = new GitHubClient(new ProductHeaderValue("Capstone"));
        Client.Credentials = basicAuth;
        var rx = new ObservableGitHubClient(Client);
        var targetBranch = "dev";

        //Since last year
        var since = DateTimeOffset.UtcNow.AddYears(-1);
        //Making commit request
        var req = new CommitRequest { Since = since, Sha = "dev" };

        //Getts the commits and then counts them
        var count = await rx.Repository.Commit
            .GetAll(Owner, Repo, req) // IObservable<GitHubCommit>
            .Where(c => c.Commit?.Author?.Date >= since)
            .Count()
            .ToTask();

        Console.WriteLine($"Commits in last year {count}");
    }
}
