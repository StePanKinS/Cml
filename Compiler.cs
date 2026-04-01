using System.Diagnostics;

namespace Cml;

public class Compiler
{
    public static ErrorReporter Compile(CmlProject project)
    {
        Dictionary<string, Lexer> lexers = new();

        string bp = project.BaseDir;
        if (!Path.IsPathRooted(bp))
            bp = Path.Combine(Directory.GetCurrentDirectory(), bp);

        foreach (var s in project.Sources)
        {
            string path = s;
            if (!Path.IsPathRooted(s))
                path = Path.Combine(bp, s);

            lexers.Add(s, new Lexer(path, s));
        }

        if (project.PrintTokens)
            printTokens(lexers);

        ErrorReporter errorer = new();
        List<FileDefinition> files = [];
        Typ.AddStandartTypes(files);

        Parser parser = new(files, errorer);
        foreach (var (path, lexer) in lexers)
        {
            parser.ParseDefinitions(lexer, path);
        }
        parser.ParseCode();

        if (errorer.Count != 0)
            return errorer;

        Directory.CreateDirectory(Path.GetDirectoryName(project.TmpBuildDir)!);
        Directory.CreateDirectory(Path.GetDirectoryName(project.Output)!);

        if (project.Backend == "fasm")
            CompileWithFasm(files, project);
        else if (project.Backend == "llvm")
            CompileWithLlvm(files, project);
        else
            throw new Exception($"Unknown backend: {project.Backend}");

        return errorer;
    }

    private static void CompileWithFasm(List<FileDefinition> files, CmlProject project)
    {
        string asm = new FasmCodeGen(files).Generate();

        string asmpath = Path.Combine(project.TmpBuildDir, $"{project.Name}.asm");
        string objpath = Path.ChangeExtension(asmpath, "o");

        Directory.CreateDirectory(Path.GetDirectoryName(asmpath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(objpath)!);

        StreamWriter sw = new(asmpath);
        sw.Write(asm);
        sw.Close();

        Process.Start(new ProcessStartInfo()
        {
            UseShellExecute = false,
            FileName = "fasm",
            Arguments = $"{asmpath} {objpath}",
            RedirectStandardOutput = true,
        })?.WaitForExit();
        Process.Start(new ProcessStartInfo()
        {
            UseShellExecute = false,
            FileName = "gcc",
            Arguments = $"{objpath} -o {project.Output} -no-pie",
            RedirectStandardOutput = true
        })?.WaitForExit();
    }

    private static void CompileWithLlvm(List<FileDefinition> files, CmlProject project)
    {
        string llvmIr = new LlvmCodeGen(files).Generate();

        string llpath = Path.Combine(project.TmpBuildDir, $"{project.Name}.ll");
        string objpath = Path.ChangeExtension(llpath, "o");

        Directory.CreateDirectory(Path.GetDirectoryName(llpath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(objpath)!);

        StreamWriter sw = new(llpath);
        sw.Write(llvmIr);
        sw.Close();

        Process.Start(new ProcessStartInfo()
        {
            UseShellExecute = false,
            FileName = "llc",
            Arguments = $"-relocation-model=pic -filetype=obj -O3 {llpath} -o {objpath}",
            // Arguments = $"-filetype=obj -O3 {llpath} -o {objpath}",
            RedirectStandardOutput = true,
        })?.WaitForExit();

        Process.Start(new ProcessStartInfo()
        {
            UseShellExecute = false,
            FileName = "gcc",
            // Arguments = $"{objpath} -o {project.Output} -no-pie",
            Arguments = $"{objpath} -o {project.Output}",
            RedirectStandardOutput = true
        })?.WaitForExit();
    }

    private static void printTokens(Dictionary<string, Lexer> lexers)
    {
        foreach ((string path, Lexer lexer) in lexers)
        {
            Console.WriteLine($"{path}:");

            foreach (var l in lexer.GetTokens())
            {
                Console.WriteLine($"  {l.Location}\t{l}");
            }

            Console.WriteLine();
        }
    }
}
