using System.CommandLine;
//options
// ✅ הגדרה בטוחה ומודרנית לכל Option
var outputOption = new Option<FileInfo>("--output", "file path and name");
outputOption.AddAlias("-o");

var languageOption = new Option<string>("--language", "list of code languages to include")
{ IsRequired = true };
languageOption.AddAlias("-l");

var noteOption = new Option<bool>("--note", "note the source of the code");
noteOption.AddAlias("-n");

var sortOption = new Option<string>("--sort", "in which order to bundle");
sortOption.AddAlias("-s");

var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "remove empty lines");
removeEmptyLinesOption.AddAlias("-r");

var authorOption = new Option<string>("--author", "writing the name of author");
authorOption.AddAlias("-a");

//command
var bundleCommand = new Command("bundle", "bundles few code files to single file");
//add options to command
bundleCommand.AddOption(outputOption);
bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(authorOption);

bundleCommand.SetHandler((FileInfo output,string lan,bool note,string sort,bool remove,string author) =>
{
    try
    {
        var currentDir = Directory.GetCurrentDirectory();
        Console.WriteLine($"📂 Current directory: {currentDir}");

        var tokens = (lan ?? "all").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string[] extensions;
        if (tokens.Length == 1 && tokens[0].Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            extensions = new[] { ".cs", ".js", ".ts", ".html", ".css" };
        }
        else
        {
            extensions = tokens
                .Select(t => t.StartsWith('.') ? t.ToLowerInvariant() : "." + t.ToLowerInvariant())
                .ToArray();
        }

    
        var allowed = new[] { ".cs", ".js", ".ts", ".html", ".css", ".json", ".md" };
        foreach (var ext in extensions)
        {
            if (!allowed.Contains(ext))
                throw new Exception($"language is not valid: {ext}");
        }

        var files = Directory.GetFiles(currentDir, "*.*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .OrderBy(f => (sort=="code")? Path.GetExtension(f):f)
            .ToArray();

        if (files.Length == 0)
        {
            Console.WriteLine("⚠️ No code files found in this directory.");
            return;
        }

        Console.WriteLine($"🧩 Found {files.Length} files. Bundling...");

        using var writer = new StreamWriter(output?.FullName ?? Path.Combine(currentDir, "bundle-output.txt"), false);

        if (author != null) { writer.WriteLine($"// ====== the author: {author} ======"); }

        foreach (var file in files)
        {
            if(note)
            writer.WriteLine($"// ====== Start of {Path.GetFileName(file)} ======");
        
            if (remove)
            File.ReadLines(file)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList()
            .ForEach(writer.WriteLine);
            else writer.WriteLine(File.ReadAllText(file));
            if (note)
            writer.WriteLine($"// ====== End of {Path.GetFileName(file)} ======\n");
        }

        Console.WriteLine($"✅ Bundle created: {output.FullName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error: {ex.Message}");
    }
}, outputOption,languageOption,noteOption,sortOption,removeEmptyLinesOption,authorOption);
// ========== create-rsp command ==========
var createRspCommand = new Command("create-rsp", "Create a response file for the 'bundle' command");

createRspCommand.SetHandler(async () =>
{
    try
    {
        Console.WriteLine("Let's create a response file (.rsp) for the 'bundle' command ✨\n");

        Console.Write("Enter response file name (without extension): ");
        var fileName = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(fileName))
        {
            Console.WriteLine("❌ Invalid file name.");
            return;
        }

        // Collect user inputs
        Console.Write("Enter output file path (e.g. result.txt): ");
        var outputPath = Console.ReadLine()?.Trim();

        Console.Write("Enter languages (space-separated, or 'all'): ");
        var languages = Console.ReadLine()?.Trim();

        Console.Write("Include note per file? (yes/no): ");
        var noteInput = Console.ReadLine()?.Trim().ToLower();
        var noteFlag = noteInput == "yes" ? "--note" : "";

        Console.Write("Sort order (type 'code' or 'name', optional): ");
        var sortOrder = Console.ReadLine()?.Trim();

        Console.Write("Remove empty lines? (yes/no): ");
        var removeEmpty = Console.ReadLine()?.Trim().ToLower();
        var removeFlag = removeEmpty == "yes" ? "--remove-empty-lines" : "";

        Console.Write("Author name (optional): ");
        var author = Console.ReadLine()?.Trim();

        // Build response file content
        var lines = new List<string>
        {
            "bundle", // the command to run
            "--language", languages ?? "all"
        };

        if (!string.IsNullOrWhiteSpace(outputPath))
            lines.AddRange(new[] { "--output", outputPath });

        if (!string.IsNullOrWhiteSpace(sortOrder))
            lines.AddRange(new[] { "--sort", sortOrder });

        if (!string.IsNullOrWhiteSpace(author))
            lines.AddRange(new[] { "--author", author });

        if (!string.IsNullOrEmpty(noteFlag))
            lines.Add(noteFlag);

        if (!string.IsNullOrEmpty(removeFlag))
            lines.Add(removeFlag);

        // Save to file
        var rspFileName = $"{fileName}.rsp";
        await File.WriteAllLinesAsync(rspFileName, lines);

        Console.WriteLine($"\n✅ Response file created: {rspFileName}");
        Console.WriteLine($"👉 You can run it with:\n   dotnet run -- @{rspFileName}\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error: {ex.Message}");
        //comment
    }
});

var rootCommand = new RootCommand("Root command for files bundler CLI ");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);
Console.WriteLine("ARGS => " + string.Join(" | ", args));

await rootCommand.InvokeAsync(args);
