using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ItemTotals.Tests;

// Reads the web page names out of the page table of the tooltip lines library copy. C# needs the list to know which
// SendMessageToPage to act on, and the library needs it to pick the shape of each tooltip, so one hand-kept list on
// each side would drift. The library is the one source and this reader proves the C# list against it.
internal static class LibraryPages
{
    // The folder name and the page id of each entry of the table, in its order.
    internal static List<(string Name, string Id)> Read()
    {
        string file = Path.Combine(Root(), "src", "Shared", "tooltip-lines", "web", "pages.ts");
        string text = File.ReadAllText(file);
        var table = Regex.Match(text, @"PAGES: PageEntry\[\] = \[(.*?)\];", RegexOptions.Singleline);
        Assert(table.Success, "no PAGES table in " + file);
        var pages = new List<(string, string)>();
        foreach (Match entry in Regex.Matches(table.Groups[1].Value, @"(?:reactive|windowPage)\(\s*'([^']+)'\s*,\s*'([^']+)'"))
            pages.Add((entry.Groups[1].Value, entry.Groups[2].Value));
        Assert(pages.Count > 0, "no entry parsed out of the PAGES table of " + file);
        return pages;
    }

    // The mod root, found from the test binary (tests/<Name>.Tests/bin/<config>/<tfm>/).
    private static string Root()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "package.json"))) dir = dir.Parent;
        Assert(dir != null, "the mod root with package.json was not found above " + Directory.GetCurrentDirectory());
        return dir.FullName;
    }

    private static void Assert(bool ok, string why)
    {
        if (!ok) throw new FileNotFoundException(why);
    }
}
