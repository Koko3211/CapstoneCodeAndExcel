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
class Program
{
    private const string Owner = "home-assistant";
    private const string Repo = "core";


    // My key rate_limit: 13:30
    // Other key rate_limit: 
    public static async Task Main()
    {
        // await Metrics.Involvement_score();
        // await Metrics.MedianReviews();
        // await Metrics.TTFSbig();

        // var contributorLogins = File.ReadAllLines(@"C:\Users\Ignac\Downloads\Cont_Names_all.txt");
        // var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        // var outPath = Path.Combine(desktop, $"activity_Names1.csv");
        // await ContributorsCheck.CheckAllActivity(outPath, contributorLogins);
        // Console.WriteLine($"Wrote: bn{outPath}");

        // await Metrics.ReviewMetrics();
        await Metrics.TTFSbig();
        // await ContributorsCheck.DuplicateEmails();
        


    }
}

