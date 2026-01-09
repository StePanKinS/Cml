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

            lexers.Add(path, new Lexer(path));
        }

        if (project.PrintTokens)
            printTokens(lexers);

        var glbNmsp = NamespaceDefinition.NewGlobal();
        Typ.AddStandartTypes(glbNmsp);
        ErrorReporter errorer = new();

        Parser parser = new(glbNmsp, errorer);
        foreach (var lexer in lexers.Values)
        {
            parser.ParseDefinitions(lexer);
        }
        parser.ParseCode();

        if (errorer.Count != 0)
            return errorer;

        string asm = new FasmCodeGen(glbNmsp).Generate();

        string asmpath = Path.Combine(project.TmpBuildDir, $"{project.Name}.asm");
        string objpath = Path.ChangeExtension(asmpath, "o");

        Directory.CreateDirectory(Path.GetDirectoryName(asmpath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(objpath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(project.Output)!);
        // Directory.CreateDirectory(objpath);
        // Directory.CreateDirectory(project.Output);

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

        return errorer;
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
