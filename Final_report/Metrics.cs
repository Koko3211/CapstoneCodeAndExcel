using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Octokit;
using Octokit.Reactive;

using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
/*
    This file contains all the metrics calculations used in the Capstone project.
    Each metric is implemented as a separate method within the Metrics class.
    Using comments I will explain all parts of the code and my reasoning. 
    I will skip commenting reoccuring parts and the fact that every method also excludes bots. 
    To preserve the original code used for collecting the data which was used in the report 
    I will not change the code though in hindsight I could have improved and optimize some parts
    I will however include comments where I think code could have been improved
    As a side note, I coded with CoPilot on and mostly used it to autofill repeating parts of the code
    and System.Console.Writeline statements.
*/
public class Metrics
{
    //Repository details
    private const string Owner = "home-assistant";
    private const string Repo = "core";


    //-----------------------------MEDIAN Reviews---------------------------------------------
    public static async Task ReviewMetrics()
    {
        //Create client and authenticate with token
        var basicAuth = new Credentials("TOKEN");
        var Client = new GitHubClient(new ProductHeaderValue("Capstone"));
        Client.Credentials = basicAuth;
        var rx = new ObservableGitHubClient(Client);

        //Set w1 and w2 timeframes
        var since = DateTimeOffset.UtcNow.AddDays(-180);
        var till = DateTimeOffset.UtcNow.AddDays(-90);

        //Make commit request for main branch
        var reqcom = new CommitRequest { Since = since, Sha = "dev" };

        //Create PR request sorted on descending creation date for optimization
        var prRequest = new PullRequestRequest
        {
            State = ItemStateFilter.Closed,
            SortProperty = PullRequestSort.Created,
            SortDirection = SortDirection.Descending,

        };
        //Set 30 day intervals
        var halfyear = DateTimeOffset.UtcNow.AddDays(-180);
        var thrDays = DateTimeOffset.UtcNow.AddDays(-30);
        var sixDays = DateTimeOffset.UtcNow.AddDays(-60);
        var nineDays = DateTimeOffset.UtcNow.AddDays(-90);
        var twelveDays = DateTimeOffset.UtcNow.AddDays(-120);
        var fifteenDays = DateTimeOffset.UtcNow.AddDays(-150);

        //Get merged prs in the past half year
        System.Console.WriteLine("Getting merged PRs");
        //Rate limit handling explained in helper methods
        var PRMerged = await WithRateLimitRetry(() => rx.PullRequest
            .GetAllForRepository(Owner, Repo, prRequest)
            .TakeWhile(pr => pr.CreatedAt >= halfyear) //Optimize to stop when older prs are reached
            .Where(pr => pr.CreatedAt >= halfyear && pr.Merged == true && !IsBot(pr.User)) //filter only merged prs and exclude bots
            .ToList()
            .ToTask());
        //Get all pr numbers in each timeframe and calculate median reviews and unique reviewers
        //MEdianCalc and UniqueReviewers explained in helper methods
        System.Console.WriteLine($"Getting 30");
        var prNum30 = PRMerged
        .Where(pr => pr.CreatedAt >= thrDays)
        .Select(pr => pr.Number)
        .ToList();
        Console.WriteLine("Calculating medians...");
        Console.WriteLine("Last 30 days:");
        await MedianCalc(prNum30);
        await UniqueReviewers(prNum30);

        System.Console.WriteLine($"Getting 60");
        var prNum60 = PRMerged
        .Where(pr => pr.CreatedAt >= sixDays && pr.CreatedAt < thrDays)
        .Select(pr => pr.Number)
        .ToList();
        Console.WriteLine("Last 60 days:");
        await MedianCalc(prNum60);
        await UniqueReviewers(prNum60);

        System.Console.WriteLine($"Getting 90");
        var prNum90 = PRMerged
        .Where(pr => pr.CreatedAt >= nineDays && pr.CreatedAt < sixDays)
        .Select(pr => pr.Number)
        .ToList();
        Console.WriteLine("Last 90 days:");
        await MedianCalc(prNum90);
        await UniqueReviewers(prNum90);

        System.Console.WriteLine($"Getting 120");
        var prNum120 = PRMerged
        .Where(pr => pr.CreatedAt >= twelveDays && pr.CreatedAt < nineDays)
        .Select(pr => pr.Number)
        .ToList();
        Console.WriteLine("Last 120 days:");
        await MedianCalc(prNum120);
        await UniqueReviewers(prNum120);

        System.Console.WriteLine($"Getting 150");
        var prNum150 = PRMerged
        .Where(pr => pr.CreatedAt >= fifteenDays && pr.CreatedAt < twelveDays)
        .Select(pr => pr.Number)
        .ToList();
        Console.WriteLine("Last 150 days:");
        await MedianCalc(prNum150);
        await UniqueReviewers(prNum150);

        System.Console.WriteLine($"Getting 180");
        var prNum180 = PRMerged
        .Where(pr => pr.CreatedAt >= halfyear && pr.CreatedAt < fifteenDays)
        .Select(pr => pr.Number)
        .ToList();
        Console.WriteLine("Last 180 days:");
        await MedianCalc(prNum180);
        await UniqueReviewers(prNum180);


    }

    //-----------------------------INVOLVEMENT SCORE---------------------------------------------
    public static async Task Involvement_score()
    {
        //Create client and authenticate with token. In hindsight I could have passed the client as a parama
        //To give the code as was used I will not change that
        var basicAuth = new Credentials("TOKEN");
        var Client = new GitHubClient(new ProductHeaderValue("Capstone"));
        Client.Credentials = basicAuth;
        var rx = new ObservableGitHubClient(Client);

        //Set timeframe
        var since = DateTimeOffset.UtcNow.AddDays(-90);
        var till = DateTimeOffset.UtcNow;

        //Same as before
        var reqcom = new CommitRequest { Since = since, Sha = "dev" };

        //Same as before
        var prRequest = new PullRequestRequest
        {
            State = ItemStateFilter.All,
            SortProperty = PullRequestSort.Created,
            SortDirection = SortDirection.Descending,
        };
        //Same as before but for issues 
        // To not repeat myself I will not comment every issue creation
        var IssueRequest = new RepositoryIssueRequest
        {
            State = ItemStateFilter.All,
            SortDirection = SortDirection.Descending,
            Since = since
        };

        //Get PRs
        Console.WriteLine("Collecting PRs");
        var prList = await WithRateLimitRetry(() => rx.PullRequest
            .GetAllForRepository(Owner, Repo, prRequest)
            .TakeWhile(pr => pr.UpdatedAt >= since)
            .Where(pr => pr.CreatedAt >= since && !IsBot(pr.User)) //&& i.CreatedAt <= till
            .ToList()
            .ToTask());

        // Get Issues
        Console.WriteLine("Collecting Issues");
        var IssueList = await WithRateLimitRetry(() => rx.Issue
            .GetAllForRepository(Owner, Repo, IssueRequest)
            .TakeWhile(i => i.UpdatedAt >= since)
            .Where(i => i.PullRequest == null && i.CreatedAt >= since && !IsBot(i.User)) //&& i.CreatedAt <= till
            .ToList()
            .ToTask());

        Console.WriteLine("Collecting Commits");
        //Get Commits
        var countCommit = await WithRateLimitRetry(() => rx.Repository.Commit
            .GetAll(Owner, Repo, reqcom)
            .Where(c => c.Commit?.Author?.Date >= since && !IsBot(c.Author)) //&& c.Commit?.Author?.Date <= till
            .ToList()
            .ToTask());
        // Group by contributor and count contributions
        Console.WriteLine("In Commits");
        var CommByCont = countCommit
                    .GroupBy(c => c.Author?.Login ?? "Unknown")
                    .Select(g => new { Contributor = g.Key, CommitCount = g.Count() })
                    .OrderByDescending(x => x.CommitCount)
                    .ToList();
        Console.WriteLine("In PRs");
        // Group by contributor and count contributions
        var PRByCont = prList
                .GroupBy(p => p.User?.Login ?? "Unknown")
                .Select(p => new { Contributor = p.Key, countPR = p.Count() })
                .OrderByDescending(x => x.countPR)
                .ToList();
        // Group by contributor and count contributions
        Console.WriteLine("In Issues");
        var IssueByCont = IssueList
                .GroupBy(p => p.User?.Login ?? "Unknown")
                .Select(p => new { Contributor = p.Key, countIssues = p.Count() })
                .OrderByDescending(x => x.countIssues)
                .ToList();
        //Write to CSV so I can perform calculations in Excel
        // Methods explained in helper methods
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        Console.WriteLine($"Writing to: {baseDir}");
        ToCsv(CommByCont, Path.Combine(baseDir, $"{Repo}_commits_90.csv"),
            ("Contributor", x => x.Contributor),
            ("CommitCount", x => x.CommitCount));

        ToCsv(PRByCont, Path.Combine(baseDir, $"{Repo}_prs_90.csv"),
            ("Contributor", x => x.Contributor),
            ("PRCount", x => x.countPR));

        ToCsv(IssueByCont, Path.Combine(baseDir, $"{Repo}_issues_90.csv"),
            ("Contributor", x => x.Contributor),
            ("IssueCount", x => x.countIssues));

    }

    //-----------------------------TIME TO FIRST RESPONSE---------------------------------------------
    //Its called big because I called 
    public static async Task TTFSbig()
    {
        var basicAuth = new Credentials("TOKEn");

        var Client = new GitHubClient(new ProductHeaderValue("Capstone"));
        Client.Credentials = basicAuth;
        var rx = new ObservableGitHubClient(Client);
        var now = DateTimeOffset.UtcNow;
        //Interval setup
        var halfyear = DateTimeOffset.UtcNow.AddDays(-180);
        var thrDays = DateTimeOffset.UtcNow.AddDays(-30);
        var sixDays = DateTimeOffset.UtcNow.AddDays(-60);
        var nineDays = DateTimeOffset.UtcNow.AddDays(-90);
        var twelveDays = DateTimeOffset.UtcNow.AddDays(-120);
        var fifteenDays = DateTimeOffset.UtcNow.AddDays(-150);

        //Make issue requests for each interval
        // Explained in helper methods
        var req30 = MakeIssueReq(thrDays);
        var req60 = MakeIssueReq(sixDays);
        var req90 = MakeIssueReq(nineDays);
        var req120 = MakeIssueReq(twelveDays);
        var req150 = MakeIssueReq(fifteenDays);
        var reqHalf = MakeIssueReq(halfyear);
        var reqc30 = MakeClosedIssueReq(thrDays);
        var reqc60 = MakeClosedIssueReq(sixDays);
        var reqc90 = MakeClosedIssueReq(nineDays);
        var reqc120 = MakeClosedIssueReq(twelveDays);
        var reqc150 = MakeClosedIssueReq(fifteenDays);
        var reqcHalf = MakeClosedIssueReq(halfyear);

        //Get issues and closed issues for each interval
        //The naming scheme is IssuesXX for all issues 
        //IssuesCXX for closed issues
        //Called methods explained in helper methods
        var Issue30 = await rx.Issue
            .GetAllForRepository(Owner, Repo, req30)
            .TakeWhile(i => i.CreatedAt >= thrDays)
            .Where(i => i.PullRequest == null && i.CreatedAt >= thrDays && i.User != null && !IsBot(i.User))
            .ToList();
        
        var IssueC30 = await  rx.Issue
            .GetAllForRepository(Owner, Repo, reqc30)
            .TakeWhile(i => i.UpdatedAt >= thrDays)
            .Where(i => i.PullRequest == null && i.User != null && i.ClosedAt >= thrDays && !IsBot(i.User))
            .ToList();

        Console.WriteLine("Calculating TTFS...");
        Console.WriteLine("Last 30 days:");
        await TTFS(Issue30);
        await IssuesCreatedVClosed(Issue30,IssueC30, thrDays, now);


        var Issue60 = await rx.Issue
            .GetAllForRepository(Owner, Repo, req60)
            .TakeWhile(pr => pr.CreatedAt >= sixDays)
            .Where(i => i.PullRequest == null && i.CreatedAt >= sixDays && i.CreatedAt <= thrDays && i.User != null && !IsBot(i.User))
            .ToList();

        var IssueC60 = await rx.Issue
            .GetAllForRepository(Owner, Repo, reqc60)
            .TakeWhile(i => i.UpdatedAt >= sixDays)
            .Where(i => i.PullRequest == null && i.ClosedAt >= sixDays && i.ClosedAt <= thrDays && i.User != null && !IsBot(i.User))
            .ToList();

        Console.WriteLine("Calculating TTFS...");
        Console.WriteLine("Last 60 days:");
        await TTFS(Issue60);
        await IssuesCreatedVClosed(Issue60,IssueC60, sixDays, thrDays);

        var Issue90 = await rx.Issue
            .GetAllForRepository(Owner, Repo, req90)
            .TakeWhile(i => i.CreatedAt >= nineDays)
            .Where(i => i.PullRequest == null && i.CreatedAt >= nineDays && i.CreatedAt <= sixDays && i.User != null && !IsBot(i.User))
            .ToList();
        var IssueC90 = await rx.Issue
            .GetAllForRepository(Owner, Repo, reqc90)
            .TakeWhile(i => i.UpdatedAt >= nineDays)
            .Where(i => i.PullRequest == null && i.ClosedAt >= nineDays && i.ClosedAt <= sixDays && i.User != null && !IsBot(i.User))
            .ToList();

        Console.WriteLine("Calculating TTFS...");
        Console.WriteLine("Last 90 days:");
        await TTFS(Issue90);
        await IssuesCreatedVClosed(Issue90, IssueC90 ,nineDays, sixDays);

        var Issue120 = await rx.Issue
            .GetAllForRepository(Owner, Repo, req120)
            .TakeWhile(i => i.CreatedAt >= twelveDays)
            .Where(i => i.PullRequest == null && i.CreatedAt >= twelveDays && i.CreatedAt <= nineDays && i.User != null && !IsBot(i.User))
            .ToList();
        var IssueC120 = await rx.Issue
            .GetAllForRepository(Owner, Repo, reqc120)
            .TakeWhile(i => i.UpdatedAt >= twelveDays)
            .Where(i => i.PullRequest == null && i.ClosedAt >= twelveDays && i.ClosedAt <= nineDays && i.User != null && !IsBot(i.User))
            .ToList();

        Console.WriteLine("Calculating TTFS...");
        Console.WriteLine("Last 120 days:");
        await TTFS(Issue120);
        await IssuesCreatedVClosed(Issue120, IssueC120, twelveDays, nineDays);

        Console.WriteLine("Getting 150");

        var Issue150 = await WithRateLimitRetry(() => rx.Issue
            .GetAllForRepository(Owner, Repo, req150)
            .TakeWhile(i => i.CreatedAt >= fifteenDays)
            .Where(i => i.PullRequest == null && i.CreatedAt >= fifteenDays && i.CreatedAt <= twelveDays && i.User != null && !IsBot(i.User))
            .ToList()
            .ToTask());
        
        var IssueC150 = await WithRateLimitRetry(() => rx.Issue
            .GetAllForRepository(Owner, Repo, reqc150)
            .TakeWhile(i => i.UpdatedAt >= fifteenDays)
            .Where(i => i.PullRequest == null && i.ClosedAt >= fifteenDays && i.ClosedAt <= twelveDays && i.User != null && !IsBot(i.User))
            .ToList()
            .ToTask());

        Console.WriteLine("Calculating TTFS...");
        Console.WriteLine("Last 150 days:");
        await TTFS(Issue150);
        await IssuesCreatedVClosed(Issue150,IssueC150, fifteenDays, twelveDays);

        var IssueHalf = await WithRateLimitRetry(() => rx.Issue
            .GetAllForRepository(Owner, Repo, reqHalf)
            .TakeWhile(i => i.CreatedAt >= halfyear)
            .Where(i => i.PullRequest == null && i.CreatedAt >= halfyear && i.CreatedAt <= fifteenDays && i.User != null && !IsBot(i.User))
            .ToList()
            .ToTask());

        var IssueCHalf = await WithRateLimitRetry(() => rx.Issue
            .GetAllForRepository(Owner, Repo, reqcHalf)
            .TakeWhile(i => i.UpdatedAt >= halfyear)
            .Where(i => i.PullRequest == null && i.ClosedAt >= halfyear && i.ClosedAt <= fifteenDays && i.User != null && !IsBot(i.User))
            .ToList()
            .ToTask());

        Console.WriteLine("Calculating TTFS...");
        Console.WriteLine("Last 180 days:");
        await TTFS(IssueHalf);
        await IssuesCreatedVClosed(IssueHalf,IssueCHalf, halfyear, fifteenDays);


    }

    // -----------------------------HELPER---------------------------------------------
    //Checks whether a users login ends with [bot]
    static bool IsBotLogin(string? login) =>
        !string.IsNullOrEmpty(login) && login.EndsWith("[bot]", StringComparison.OrdinalIgnoreCase);

    //I had to make two overloads because User and Author are different types
    //These check whether user has type bot and login
    static bool IsBot(User? u) =>
        u != null && (u.Type == AccountType.Bot || IsBotLogin(u.Login));

    static bool IsBot(Author? a) =>
        a != null && (string.Equals(a.Type, "Bot", StringComparison.OrdinalIgnoreCase) || IsBotLogin(a.Login));

    //This and the Csv method were created with the help of ChatGPT as this is my first time (except for the interim report) using C#.
    //With exam season creeping in I believe that this is a fair use of LLMs as this is not related to any lectures.
    public static void ToCsv<T>(
        IEnumerable<T> data,
        string path,
        params (string Header, Func<T, object?> Selector)[] cols)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using (var sw = File.CreateText(path)) // no-arg, cross-version safe
        {
            // header
            sw.WriteLine(string.Join(",", cols.Select(c => Csv(c.Header))));

            // rows
            foreach (var item in data)
            {
                var fields = cols.Select(c => Csv(c.Selector(item)?.ToString() ?? string.Empty));
                sw.WriteLine(string.Join(",", fields));
            }
        }
    }

    public static string Csv(string s)
    {
        // RFC-4180 style quoting
        if (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    //-----------------------------MEDIAN CALCULATION---------------------------------------------
    public static async Task MedianCalc(List<int>? prNum)
    {
        var basicAuth = new Credentials("TOKEn");
        var Client = new GitHubClient(new ProductHeaderValue("Capstone"));
        Client.Credentials = basicAuth;
        var rx = new ObservableGitHubClient(Client);

        //Create list to store number of reviews per PR
        var reviewCounts = new List<int>();
        if (prNum == null) { System.Console.WriteLine("No PRs found."); return; }
        foreach (var pr in prNum)
        {
            //Get all reviews for the PR num 
            var reviewCount = await WithRateLimitRetry(() => rx.PullRequest.Review.GetAll(Owner, Repo, pr)
            .Where(r => !IsBot(r.User))
            .Count()
            .ToTask());
            reviewCounts.Add(reviewCount);
        }
        //Sort so we can get median
        reviewCounts.Sort();
        double median;
        int count = reviewCounts.Count;
        int sum = reviewCounts.Sum();
        float mean = sum / count;
        //Gets median
        if (count % 2 == 0)
        {
            // If even, average the two middle elements
            median = (reviewCounts[count / 2 - 1] + reviewCounts[count / 2]) / 2.0;
        }
        else
        {
            // If odd, take the middle element
            median = reviewCounts[count / 2];
        }
        System.Console.WriteLine($"Median Rev: {median}, Mean rev: {mean}");
    }

    //-----------------------------TIME TO FIRST RESPONSE---------------------------------------------
    public static async Task TTFS(IList<Issue> Issues)
    {
        var basicAuth = new Credentials("Token");
        var Client = new GitHubClient(new ProductHeaderValue("Capstone"));
        Client.Credentials = basicAuth;
        var rx = new ObservableGitHubClient(Client);

        //List to store time to first response for each issue
        var timeToFirstResponses = new List<TimeSpan>();
        foreach (var issue in Issues)
        {
            //Gets comments per issue number 
            var comments = await WithRateLimitRetry(() => rx.Issue.Comment.GetAllForIssue(Owner, Repo, issue.Number)
                .ToList()
                .ToTask());
            //Safe guard just in case but unlikely. VScode does not like possible null reference and highlights them which annoyed me
            if (comments.Count > 0)
            {
                //Order by first comments and calculate time to first response
                var firstComment = comments.OrderBy(c => c.CreatedAt).First();
                var timeToFirstResponse = firstComment.CreatedAt - issue.CreatedAt;
                timeToFirstResponses.Add(timeToFirstResponse);
            }
        }
        //Get average
        var averageTime = new TimeSpan(Convert.ToInt64(timeToFirstResponses.Average(ts => ts.Ticks)));
        Console.WriteLine($"Average time to first response: {averageTime}");

    }
    //-----------------------------MAKE ISSUE REQ---------------------------------------------
    //To not create these by hand, which was tedious, for TTSF I made the helper method
    public static RepositoryIssueRequest MakeIssueReq(DateTimeOffset since)
    {
        var IssueRequest = new RepositoryIssueRequest
        {
            State = ItemStateFilter.All,
            SortDirection = SortDirection.Descending,
            Since = since
        };
        return IssueRequest;

    }
    //Same as above but for closed issues
    public static RepositoryIssueRequest MakeClosedIssueReq(DateTimeOffset since)
    {
        var IssueRequest = new RepositoryIssueRequest
        {
            State = ItemStateFilter.Closed,
            SortProperty = IssueSort.Updated,
            SortDirection = SortDirection.Descending,
            Since = since
        };
        return IssueRequest;
    }
    //-----------------------------Get Unique Reviewers number---------------------------------------------
    public static async Task UniqueReviewers(List<int>? prNum)
    {
        var basicAuth = new Credentials("TOKEN");
        var Client = new GitHubClient(new ProductHeaderValue("Capstone"));
        Client.Credentials = basicAuth;
        var rx = new ObservableGitHubClient(Client);

        int reviewers = 0;
        //Create a hash set to store and check whether a reviewer has already been counted
        var seen = new HashSet<string>();
        if (prNum == null) { System.Console.WriteLine("No PRs found."); return; }
        //Go through all pr numbers and get all reviewers
        foreach (var pr in prNum)
        {
            var reviewPR = await WithRateLimitRetry(() => rx.PullRequest.Review.GetAll(Owner, Repo, pr)
            .Where(r => !IsBot(r.User))
            .Select(r => r.User?.Login)
            .Distinct()
            .ToList()
            .ToTask());

            if (reviewPR == null) continue;
            foreach (var rev in reviewPR)
            {
                if (rev == null) continue;
                //If reviewer not seen yet add to hashset and increment count
                if (seen.Add(rev)) reviewers++;
            }

        }
        System.Console.WriteLine($" Unique reviewers: {reviewers}");

    }

    //-----------------------------Issues OpenVClosed---------------------------------------------
    public static async Task IssuesCreatedVClosed(IList<Issue> IssuesCreated,IList<Issue> IssuesClosed, DateTimeOffset? since, DateTimeOffset? till )
    {

        int AllIssues = IssuesCreated.Count();
        int closedCount = IssuesClosed.Count();
        Console.WriteLine($"All Issues: {AllIssues}, Closed in the frame Issues: {closedCount}");
        Console.WriteLine($"Closure rate: {(double)closedCount / AllIssues}");

    }

        static async Task<T> WithRateLimitRetry<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (RateLimitExceededException ex)
        {
            var delay = ex.Reset - DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1);
            if (delay < TimeSpan.Zero) delay = TimeSpan.FromSeconds(2);
            Console.WriteLine($"Rate limited; sleeping {delay:c} (reset {ex.Reset:u})");
            await Task.Delay(delay);
            return await action();
        }
        catch (AbuseException)
        {
            var delay = TimeSpan.FromSeconds(60);
            Console.WriteLine($"Secondary limit; sleeping {delay:c}");
            await Task.Delay(delay);
            return await action();
        }
        catch  (Exception)
        {
            return await action();
        }
    }


//     static async Task<T> WithRateLimitRetry<T>(
//         Func<Task<T>> action, int maxRetries = 10, CancellationToken ct = default)
//     {
//         for (var attempt = 0; attempt <= maxRetries; attempt++)
//         {
//             try { return await action(); }
//             catch (RateLimitExceededException ex) when (attempt < maxRetries)
//             {
//                 var delay = ex.Reset - DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1);
//                 if (delay < TimeSpan.Zero) delay = TimeSpan.FromSeconds(2);
//                 await Task.Delay(delay, ct);
//                 continue;
//             }
//             catch (AbuseException) when (attempt < maxRetries)
//             {
//                 var backoff = TimeSpan.FromSeconds(30 * (attempt + 1)); // linear/exponential if you want
//                 await Task.Delay(backoff, ct);
//                 continue;
//             }
//             catch (ApiException ex)
//             {
//                 // Log details to understand what's wrong
//                 LogApiException(ex);

//                 // Retry only on transient server/network problems
//                 if (IsTransient(ex) && attempt < maxRetries)
//                 {
//                     var backoff = TimeSpan.FromSeconds(Math.Pow(2, attempt + 1)); // 2s, 4s, 8s...
//                     Console.WriteLine($"Transient {((int)ex.StatusCode)}. Retrying in {backoff:c}...");
//                     await Task.Delay(backoff, ct);
//                     continue;
//                 }

//                 // Otherwise, bubble up (403/404/422 etc. are usually permanent)
//                 throw;
//             }
//         }
//         // If we get here, we already retried maxRetries times
//         return await action(); // let any exception bubble
//     }
//     static bool IsTransient(ApiException ex)
// {
//         // treat these as retryable
//         return ex.StatusCode is HttpStatusCode.RequestTimeout       // 408
//             or HttpStatusCode.BadGateway                             // 502
//             or HttpStatusCode.ServiceUnavailable                     // 503
//             or HttpStatusCode.GatewayTimeout  
//             or HttpStatusCode.RequestTimeout;                      // 504;
// }

// static void LogApiException(ApiException ex)
// {
//     var apiErr = ex.ApiError;
//     Console.WriteLine($"GitHub API error {((int)ex.StatusCode)} {ex.StatusCode}: {apiErr?.Message ?? ex.Message}");
//     if (!string.IsNullOrWhiteSpace(apiErr?.DocumentationUrl))
//         Console.WriteLine($"  docs: {apiErr.DocumentationUrl}");
//     if (apiErr?.Errors != null)
//     {
//         foreach (var e in apiErr.Errors)
//             Console.WriteLine($"  detail: {e.Message} (resource={e.Resource}, field={e.Field}, code={e.Code})");
//     }
// }
}