using Newtonsoft.Json;

#region Console Intro Junk

using System.Net;
using System.Net.Http.Headers;

Console.WindowWidth = 150;

Console.ForegroundColor = ConsoleColor.Green;

Console.WriteLine("\n\n");

Console.WriteLine(@" /$$$$$$           /$$      /$$            /$$$$$$  /$$   /$$ /$$$$$$$                                /$$                     /$$      /$$          
|_  $$_/          | $$$    /$$$           /$$__  $$| $$  | $$| $$__  $$                              |__/                    | $$$    /$$$          
  | $$    /$$$$$$$| $$$$  /$$$$ /$$   /$$| $$  \__/| $$  | $$| $$  \ $$  /$$$$$$  /$$   /$$ /$$   /$$ /$$ /$$$$$$$   /$$$$$$ | $$$$  /$$$$  /$$$$$$ 
  | $$   /$$_____/| $$ $$/$$ $$| $$  | $$| $$ /$$$$| $$$$$$$$| $$  | $$ /$$__  $$|  $$ /$$/|  $$ /$$/| $$| $$__  $$ /$$__  $$| $$ $$/$$ $$ /$$__  $$
  | $$  |  $$$$$$ | $$  $$$| $$| $$  | $$| $$|_  $$| $$__  $$| $$  | $$| $$  \ $$ \  $$$$/  \  $$$$/ | $$| $$  \ $$| $$  \ $$| $$  $$$| $$| $$$$$$$$
  | $$   \____  $$| $$\  $ | $$| $$  | $$| $$  \ $$| $$  | $$| $$  | $$| $$  | $$  >$$  $$   >$$  $$ | $$| $$  | $$| $$  | $$| $$\  $ | $$| $$_____/
 /$$$$$$ /$$$$$$$/| $$ \/  | $$|  $$$$$$$|  $$$$$$/| $$  | $$| $$$$$$$/|  $$$$$$/ /$$/\  $$ /$$/\  $$| $$| $$  | $$|  $$$$$$$| $$ \/  | $$|  $$$$$$$
|______/|_______/ |__/     |__/ \____  $$ \______/ |__/  |__/|_______/  \______/ |__/  \__/|__/  \__/|__/|__/  |__/ \____  $$|__/     |__/ \_______/
                                /$$  | $$                                                                           /$$  \ $$                       
                               |  $$$$$$/                                                                          |  $$$$$$/                       
                                \______/                                                                            \______/                        ");

Console.ForegroundColor = ConsoleColor.White;

#endregion

#region variables

int argCount = 0;
int nameLeakCount = 0;
int emailLeakCount = 0;
string githubRepo = "";
string targetEmail = "";
string targetName = "";
string targetBranch = "";
bool leaked = false;

#endregion

#region Argument Parsing

foreach (string arg in args)
{
    try
    {
        switch (arg)
        {
            case "--github-repository":
                githubRepo = args[argCount + 1].Replace('"', ' ');
                break;
            case "--target-email":
                targetEmail = args[argCount + 1].Replace('"', ' '); ;
                break;
            case "--target-name":
                targetName = args[argCount + 1].Replace('"', ' '); ;
                break;
            case "--target-branch":
                targetBranch = args[argCount + 1].Replace('"', ' '); ;
                break;
        }
        argCount++;
    }
    catch { }
}

if (args.Length < 5 || string.IsNullOrEmpty(githubRepo) || string.IsNullOrEmpty(targetEmail) || string.IsNullOrEmpty(targetName) || string.IsNullOrEmpty(targetBranch))
{
    Console.WriteLine(
        "\n\n" +
        "Usage: \n\n" +
        "--github-repository \"https://github.com/user/example\"\n" +
        "--target-email \"email.com\"\n" +
        "--target-name \"name here\"\n" +
        "--target-branch \"branch\""
        );
    Environment.Exit(0);
}

Console.WriteLine(
        "Current Configuration: \n\n" +
        $"GitHub Repo: {githubRepo}\n" +
        $"Target Name: {targetName}\n" +
        $"Target Email: {targetEmail}\n" +
        $"Target Branch: {targetBranch}\n " +
        "\n"
    );

#endregion

#region Get All Commits And Parse For Name And Emails

string Data = DownloadString($"https://api.github.com/repos/{githubRepo.Replace("https://github.com/","")}/commits?sha={targetBranch}");
List<Root> parsedJSON = JsonConvert.DeserializeObject<List<Root>>(Data);
foreach (Root parsedData in parsedJSON)
{
    // Email Detection

    if (Contains(parsedData.commit.committer.email, targetEmail) || Contains(parsedData.commit.author.email, targetEmail) || Contains(parsedData.commit.message, targetName))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"<!> Email Leaked In Commit: {parsedData.html_url}");
        emailLeakCount++;
        leaked = true;
    }

    // Name Detection

    if (Contains(parsedData.commit.committer.name, targetName) || Contains(parsedData.commit.author.name, targetName) || Contains(parsedData.commit.message, targetName))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"<!> Name Leaked In Commit: {parsedData.html_url}");
        nameLeakCount++;
        leaked = true;
    }
}

// Present A Message If Nothing Has Leaked And Give A Count Of Leaks If There Was Any

if (leaked == false)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("<!> Good News! According To The Program Nothing Has Been Leaked!");
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(
            $"<!> There has been {nameLeakCount + emailLeakCount} Leaks.\n" +
            $"<!> {nameLeakCount} of them being name leaks.\n" +
            $"<!> {emailLeakCount} of them being email leaks."
                     );
}

// Reset Colour

Console.ForegroundColor = ConsoleColor.White;

// Exit

Environment.Exit(0);

#endregion

#region Misc Methods


static string DownloadString(string Url)
{
    HttpClient hc = new();
    hc.Timeout = Timeout.InfiniteTimeSpan;
    hc.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("IsMyGHDoxxingMe", "1"));
    HttpResponseMessage hrm4 = hc.GetAsync(Url).GetAwaiter().GetResult();
    using (HttpContent content = hrm4.Content)
    {
        return content.ReadAsStringAsync().Result;
    }
}

// Stolen from stackoverflow https://stackoverflow.com/questions/444798/case-insensitive-containsstring
static bool Contains(string source, string toCheck)
{
    return source?.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
}

#endregion

Thread.Sleep(-1);

#region Classes To Split The API Output
public class Author
{
    public string name { get; set; }
    public string email { get; set; }
    public DateTime date { get; set; }
    public string login { get; set; }
    public int id { get; set; }
    public string node_id { get; set; }
    public string avatar_url { get; set; }
    public string gravatar_id { get; set; }
    public string url { get; set; }
    public string html_url { get; set; }
    public string followers_url { get; set; }
    public string following_url { get; set; }
    public string gists_url { get; set; }
    public string starred_url { get; set; }
    public string subscriptions_url { get; set; }
    public string organizations_url { get; set; }
    public string repos_url { get; set; }
    public string events_url { get; set; }
    public string received_events_url { get; set; }
    public string type { get; set; }
    public bool site_admin { get; set; }
}

public class Commit
{
    public Author author { get; set; }
    public Committer committer { get; set; }
    public string message { get; set; }
    public Tree tree { get; set; }
    public string url { get; set; }
    public int comment_count { get; set; }
    public Verification verification { get; set; }
}

public class Committer
{
    public string name { get; set; }
    public string email { get; set; }
    public DateTime date { get; set; }
    public string login { get; set; }
    public int id { get; set; }
    public string node_id { get; set; }
    public string avatar_url { get; set; }
    public string gravatar_id { get; set; }
    public string url { get; set; }
    public string html_url { get; set; }
    public string followers_url { get; set; }
    public string following_url { get; set; }
    public string gists_url { get; set; }
    public string starred_url { get; set; }
    public string subscriptions_url { get; set; }
    public string organizations_url { get; set; }
    public string repos_url { get; set; }
    public string events_url { get; set; }
    public string received_events_url { get; set; }
    public string type { get; set; }
    public bool site_admin { get; set; }
}

public class Parent
{
    public string sha { get; set; }
    public string url { get; set; }
    public string html_url { get; set; }
}

public class Root
{
    public string sha { get; set; }
    public string node_id { get; set; }
    public Commit commit { get; set; }
    public string url { get; set; }
    public string html_url { get; set; }
    public string comments_url { get; set; }
    public Author author { get; set; }
    public Committer committer { get; set; }
    public List<Parent> parents { get; set; }
}

public class Tree
{
    public string sha { get; set; }
    public string url { get; set; }
}

public class Verification
{
    public bool verified { get; set; }
    public string reason { get; set; }
    public string signature { get; set; }
    public string payload { get; set; }
}

#endregion
