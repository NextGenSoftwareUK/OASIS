using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.BlueSkyOASIS.TestHarness
{
    /// <summary>
    /// Console harness against a real PDS. Configure with
    /// BLUESKYOASIS_PDS_URL, BLUESKYOASIS_IDENTIFIER, BLUESKYOASIS_PASSWORD and BLUESKYOASIS_ENCRYPTION_KEY_HEX (64 hex chars).
    ///
    ///   save &lt;holon-guid&gt; &lt;name&gt;   saves a holon
    ///   load &lt;holon-guid&gt;          loads it and prints its name (e.g. after restarting the PDS)
    ///   delete &lt;holon-guid&gt;        deletes it
    ///   post &lt;text&gt;               publishes an app.bsky.feed.post, then deletes it
    /// Exit code 0 on success, 1 on any failure.
    /// </summary>
    public static class BlueSkyOASISTestHarness
    {
        private static string Env(string name) => Environment.GetEnvironmentVariable(name)
            ?? throw new InvalidOperationException($"{name} is required.");

        public static async Task<int> Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("usage: save <guid> <name> | load <guid> | delete <guid> | post <text>");
                return 1;
            }
            using var provider = new BlueSkyOASIS(new Uri(Env("BLUESKYOASIS_PDS_URL")), Env("BLUESKYOASIS_IDENTIFIER"),
                Env("BLUESKYOASIS_PASSWORD"), Convert.FromHexString(Env("BLUESKYOASIS_ENCRYPTION_KEY_HEX")));

            var activated = await provider.ActivateProviderAsync();
            if (activated.IsError) return Fail("activate", activated.Message);
            try
            {
                switch (args[0])
                {
                    case "save" when args.Length >= 3:
                        var saved = await provider.SaveHolonAsync(new Holon { Id = Guid.Parse(args[1]), Name = args[2] });
                        if (saved.IsError) return Fail("save", saved.Message);
                        Console.WriteLine($"PASS  saved {args[1]}");
                        return 0;
                    case "load":
                        var loaded = await provider.LoadHolonAsync(Guid.Parse(args[1]));
                        if (loaded.IsError || loaded.Result == null) return Fail("load", loaded.Message);
                        Console.WriteLine($"PASS  loaded {loaded.Result.Id} name={loaded.Result.Name}");
                        return 0;
                    case "delete":
                        var deleted = await provider.DeleteHolonAsync(Guid.Parse(args[1]));
                        if (deleted.IsError) return Fail("delete", deleted.Message);
                        Console.WriteLine($"PASS  deleted {args[1]}");
                        return 0;
                    case "post":
                        var post = await provider.PublishPostAsync(args[1]);
                        if (post.IsError) return Fail("post", post.Message);
                        Console.WriteLine($"PASS  posted {post.Result}");
                        var removed = await provider.DeletePostAsync(post.Result!);
                        if (removed.IsError) return Fail("delete post", removed.Message);
                        Console.WriteLine("PASS  deleted post");
                        return 0;
                    default:
                        return Fail("arguments", $"unknown command '{args[0]}'");
                }
            }
            finally
            {
                await provider.DeActivateProviderAsync();
            }
        }

        private static int Fail(string step, string message)
        {
            Console.Error.WriteLine($"FAIL  {step}: {message}");
            return 1;
        }
    }
}
