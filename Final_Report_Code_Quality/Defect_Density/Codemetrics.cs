using System.Reactive.Linq;
using Octokit;
using Octokit.Reactive;
using System.IO;

// This class queries GitHub issues from the Home Assistant core repository
// and extracts information about the integrations causing the issues.
// The extracted data is formatted as CSV and saved to a text file.

public class Codemetrics
{
    private const string Owner = "home-assistant";
    private const string Repo = "core";
    public static async Task Issuee()
    {
        var basicAuth = new Credentials("TOKEN");
        var client = new GitHubClient(new ProductHeaderValue("Capstone"));
        client.Credentials = basicAuth;
        var rx = new ObservableGitHubClient(client);
        // Time window — issues created in the last 24 hours
        DateTimeOffset since = DateTimeOffset.UtcNow.AddDays(-365);

        // Request + paging (GitHub pages results; Rx will stream them)
        var request = new RepositoryIssueRequest
        {
            State = ItemStateFilter.All,
            Since = since       // NOTE: filters by *updated since*; we'll filter CreatedAt below
        };
        var paging = new ApiOptions { PageSize = 100, PageCount = 10, StartPage = 1 };

        // Stream issues reactively:
        // - Flatten pages
        // - Exclude PRs (Search returns both, Issues API too)
        // - Keep only those actually created in the last day
        var issueStream =
            rx.Issue
              .GetAllForRepository(Owner, Repo, request) // IObservable<IReadOnlyList<Issue>
              .TakeWhile(i => i.CreatedAt >= since)      // stop when we reach older issues
              .Where(i => i.PullRequest == null)                // real issues only
              .Where(i => i.CreatedAt > since);

        int count = 0; //Count processed issues

        string csvFormat = ""; //Store integrations in CSV format

        await issueStream.ForEachAsync(issue =>
        {
            count++;
            if (count % 100 == 0)
            {
                Console.WriteLine($"Processed {count} issues...");
            }

            string body = issue.Body; //Get issue body text
            if (body != null)
            {

                string[] lines = body.Split('\n'); //Split body into lines
                //Find the line where the integration causing the issue is mentioned
                int i = 0;
                while (lines[i] != "### Integration causing the issue" && i < lines.Length - 1)
                {
                    i++;
                }
                if (i < lines.Length - 1)
                {
                    string[] words = lines[i + 2].Split(' ');
                    bool multi = false;
                    for (int f = 0; f < words.Length; f++)
                    {
                        if (words[f] == "and" || words[f] == "or")
                        {
                            csvFormat += words[f - 1].Trim() + "," + words[f + 1].Trim();
                            multi = true;
                        }
                    }
                    if (!multi)
                    {
                        //Add the integration to the CSV string, or a placeholder if not specified
                        csvFormat += string.IsNullOrWhiteSpace(lines[i + 2]) ? "(no description), " : lines[i + 2] + ", ";
                    }
                }
                else
                {
                    //If the integration line is not found, add a placeholder
                    csvFormat += "no integration specified/Wrong format, ";
                }

            }
        });
        //Write the CSV string to a text file
        StreamWriter sw = new StreamWriter("C:/Users/cassi/OneDrive/Dokumentumok/Uni stuff/Capstone/igibs/igiBsproject/issueCount.txt");
        sw.WriteLine(csvFormat);
        sw.Close();
        Console.WriteLine("Finished");
    }
}