using System;
using System.IO;
using System.Linq;
using System.Text;
using Octokit;
using Octokit.Internal;

namespace octocli.net
{
    class Program
    {
        static GitHubClient github;
        static string org;
        static void Main(string[] args)
        {
            Console.WriteLine("This CLI will target user, teams, and repository management via the GitHub API.");

            Console.Write("Enter the organization you are managing: ");
            org = Console.ReadLine();

            Console.Write("Enter the user that will be making API calls: ");
            var user = Console.ReadLine();

            Console.Write("Enter your password for authentication: ");
            string password = "";
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }
                password += key.KeyChar;
            }
            Console.WriteLine();

            github = new GitHubClient(
                new ProductHeaderValue(org),
                new InMemoryCredentialStore(new Credentials(user, password))
                );

            ListOptions();
            while (Run(Console.ReadLine()))
            {
                ListOptions();
            }
        }

        static bool Run(string input)
        {
            switch(Convert.ToInt32(input))
            {
                case 1:
                    UserOptions();
                    return true;
                case 2:
                    TeamOptions();
                    return true;
                case 3:
                    RepositoryOptions();
                    return true;
                default:
                    // done executing
                    return false;
            }
        }

        static void ListOptions()
        {
            Console.WriteLine("\\\\\\\\ GitHub Management Options ////");
            Console.WriteLine("\t1. Manage a user");
            Console.WriteLine("\t2. Manage a team");
            Console.WriteLine("\t3. Manage repositories");
        }

        static void UserOptions()
        {
            Console.WriteLine("User options:");
            Console.WriteLine("1. List users in org");
            Console.WriteLine("2. Manage a user");

            switch(Convert.ToInt32(Console.ReadLine()))
            {
                case 1:
                    var users = github.Organization.Member.GetAll(org).Result;
                    Console.WriteLine($"Users in org {org}");
                    foreach(var u in users)
                    {
                        Console.WriteLine($"{u.Name} - {u.Login}");
                    }

                    Console.WriteLine("\n\nDo you want to output this list for use? (y/n)");

                    if (Console.ReadLine().Equals("y", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Console.WriteLine("Writing to ./users-out.txt");
                        StringBuilder sb = new StringBuilder();
                        sb.AppendJoin(',', users.Select(u => u.Login));

                        File.WriteAllText("./users-out.txt", sb.ToString());
                    }
                    break;

                case 2:
                    Console.Write("Enter the user you would like to manage: ");
                    string user = Console.ReadLine();

                    bool isMember = github.Organization.Member.CheckMember(org, user).Result;
                    if (!isMember)
                    {
                        Console.WriteLine($"User {user} is not a member of org {org}. Unsupported operations.");
                        return;
                    }
                    break;
                default:
                    break;
            }
        }

        static void TeamOptions()
        {
            var teams = github.Organization.Team.GetAll(org).Result;
            Console.WriteLine("Teams available for your org: ");
            int index = 0;
            foreach (var team in teams)
            {
                Console.WriteLine($"{index}: {team.Name} - {team.Description}");
                index++;
            }

            Console.WriteLine("\nSelect a team: ");
            int teamIndex = Convert.ToInt32(Console.ReadLine());

            Console.WriteLine("Team management options: ");
            Console.WriteLine("1. Add user/s");
            Console.WriteLine("2. Add repositories");

            switch(Convert.ToInt32(Console.ReadLine()))
            {
                case 1:
                    Console.WriteLine("Enter user/s to add to the team. For multiple users, use a comma separated list");
                    Console.WriteLine("Note: only users in the org will be added.");
                    TeamAddUsers(Console.ReadLine(), teams[teamIndex]);
                    break;
                case 2:
                    Console.WriteLine("Enter the repositories to add to the team. For multiple repositories, use a comma separated list");
                    TeamAddRepositories(Console.ReadLine(), teams[teamIndex]);
                    break;
                default:
                    break;
            }
            
        }

        static void TeamAddUsers(string users, Team team)
        {
            foreach (string user in users.Split(","))
            {
                if (string.IsNullOrWhiteSpace(user))
                {
                    continue;
                }

                bool isMember = github.Organization.Member.CheckMember(org, user.Trim()).Result;
                if (!isMember)
                {
                    Console.WriteLine($"User {user} is not in the org and will not be added to team.");
                    continue;
                }

                UpdateTeamMembership membershipOptions = new UpdateTeamMembership(TeamRole.Member);
                github.Organization.Team.AddOrEditMembership(team.Id, user.Trim(), membershipOptions);
            }
        }

        static void TeamAddRepositories(string repositories, Team team)
        {
            RepositoryPermissionRequest permissionRequest = new RepositoryPermissionRequest(Permission.Pull); // does not actually correspond to permission options in github

            foreach(var repo in repositories.Split(","))
            {
                if (string.IsNullOrWhiteSpace(repo))
                {
                    continue;
                }

                github.Organization.Team.AddRepository(team.Id, org, repo);
            }
        }

        static void RepositoryOptions()
        {
            Console.WriteLine("Repository options:");
            Console.WriteLine("1. List repositories in org");

            if (1 == Convert.ToInt32(Console.ReadLine()))
            {
                var repos = github.Repository.GetAllForOrg(org).Result;
                Console.WriteLine($"Repos in org {org}");
                foreach(var repo in repos)
                {
                    Console.WriteLine($"{repo.FullName}");
                }

                Console.WriteLine("\n\nDo you want to output this list for use? (y/n)");

                if (Console.ReadLine().Equals("y", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine("Writing to ./repos-out.txt");
                    StringBuilder sb = new StringBuilder();
                    sb.AppendJoin(',', repos.Select(r => r.Name));

                    File.WriteAllText("./repos-out.txt", sb.ToString());
                }
            }
        }
    }
}
