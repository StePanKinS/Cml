using System.Text;

namespace Cml.CodeGenerating;

public class FasmCodeGen(NamespaceDefinition globalNamespace) //, ErrorReporter errorer)
{
    private NamespaceDefinition globalNamespace = globalNamespace;
    private List<string> strings = [];

    public string Generate()
        => topLevel();

    private string topLevel()
    {
        StringBuilder sb = new();
        sb.Append("format ELF64\n\n");
        sb.Append("section '.text' executable writeable\n");

        generateNamespace(globalNamespace, sb);

        sb.Append("section '.data' writeable\n");
        generateStrings(sb);

        return sb.ToString();
    }

    private void generateStrings(StringBuilder sb)
    {
        for (int i = 0; i < strings.Count; i++)
        {
            string s = strings[i];
            sb.Append($"str{i} db ");
            foreach (char c in s)
            {
                if (c == '\\')
                    sb.Append("92, ");
                else if (c == '"')
                    sb.Append("34, ");
                else if (c == '\n')
                    sb.Append("10, ");
                else if (c == '\r')
                    sb.Append("13, ");
                else if (c == '\t')
                    sb.Append("9, ");
                else if (c < 32 || c > 126)
                    sb.Append($"{Convert.ToInt32(c)}, ");
                else
                    sb.Append($"'{c}', ");
            }
            sb.Append("0\n");
        }
        sb.Append('\n');
    }

    private void generateNamespace(NamespaceDefinition nmsp, StringBuilder sb)
    {
        foreach (var def in nmsp)
        {
            switch (def)
            {
                case NamespaceDefinition ns:
                    generateNamespace(ns, sb);
                    break;
                case FunctionDefinition fn:
                    generateFunction(fn, sb);
                    break;
            }
        }
    }

    private void generateFunction(FunctionDefinition fn, StringBuilder sb)
    {
        if (fn.Modifyers.Contains(Keywords.External))
        {
            sb.Append($"; External function {fn.FullName}\n");
            sb.Append($"extrn {fn.Name}\n\n");
            return;
        }
        if (fn.Modifyers.Contains(Keywords.Export))
        {
            sb.Append($"; Exported function {fn.FullName}\n");
            sb.Append($"public {fn.Name}\n\n");
        }
        if (fn.Code == null)
            throw new Exception($"Function {fn.FullName} has no code");


        sb.Append($"; Function {fn.FullName}\n");
        sb.Append($"{fn.FullName}:\n");

        sb.Append("    push rbp\n");
        sb.Append("    mov rbp, rsp\n");

        generateExecutable(fn.Code, fn.Arguments, sb);

        sb.Append("    pop rbp\n");
        sb.Append("    ret\n");
        sb.Append('\n');
    }

    private int generateExecutable(Executable exe, INameContainer locals, StringBuilder sb)
    {
        switch (exe)
        {
            case CodeBlock cb:
                generateCodeBlock(cb, locals, sb);
                return 0;
            case FunctionCall fc:
                generateFunctioncall(fc, locals, sb);
                return 0;
            case Identifyer id:
                return generateIdentifyer(id, locals, sb);
            case Literal<string> stringLit:
                sb.Append($"    ; String literal {stringLit.Location}\n");
                int count = strings.Count;
                strings.Add(stringLit.Value);
                sb.Append($"    lea rax, [str{count}]\n");
                return 0;
            case Literal<ulong> intLit:
                sb.Append($"    ; Integer literal {intLit.Location}\n");
                sb.Append($"    mov rax, {intLit.Value}\n");
                return 0;
            default:
                throw new NotImplementedException($"Code generation for {exe.GetType().Name} not implemented");
        }
    }

    private void generateCodeBlock(CodeBlock cb, INameContainer locals, StringBuilder sb)
    {
        if (cb.ReturnType.Size > 8)
            throw new Exception("Return type size greater than 8 bytes not supported");

        sb.Append($"    ; Code block start {cb.Location}\n");
        // ensure stack allocation is 16-byte aligned
        int alloc = Align(cb.Locals.Size + cb.ReturnType.Size, 16);
        sb.Append($"    sub rsp, {alloc}\n"); ;
        foreach (var c in cb.Code)
            generateExecutable(c, locals, sb);

        sb.Append($"    ; Code block end {cb.Location}\n");
        // sb.Append($"    pop rax\n"); // Expected code block return value on top of the stack
        sb.Append($"    add rsp, {alloc}\n");
    }

    private void generateFunctioncall(FunctionCall fc, INameContainer locals, StringBuilder sb)
    {
        sb.Append($"    ; Function call {fc.Location}\n");
        // SysV AMD64 calling convention: first 6 integer/pointer args in registers (rdi,rsi,rdx,rcx,r8,r9),
        // remaining args pushed on the stack. Evaluate args in the same order as before (last -> first).
        string[] regs = new[] { "rdi", "rsi", "rdx", "rcx", "r8", "r9" };
        int totalArgs = fc.Args.Length;
        int stackArgs = Math.Max(0, totalArgs - 6);
        int stackBytes = stackArgs * 8;
        // To keep RSP 16-byte aligned at the call, if stackBytes % 16 == 8 we need an extra 8-byte pad.
        int pad = (stackBytes % 16 == 8) ? 8 : 0;
        if (pad > 0)
            sb.Append($"    sub rsp, {pad}    ; call alignment pad\n");

        for (int i = totalArgs - 1; i >= 0; i--)
        {
            if (generateExecutable(fc.Args[i], locals, sb) != 0)
                throw new Exception("Error in argument evaluation");

            if (i < 6)
            {
                sb.Append($"    mov {regs[i]}, rax\n");
            }
            else
            {
                sb.Append("    push rax\n");
            }
        }

        if (generateExecutable(fc.FunctionPointer, locals, sb) != 0)
            throw new Exception("Error in function pointer calculation");
        sb.Append("    call rax\n");
        // clean up pushed stack arguments and any padding
        if (stackBytes + pad > 0)
            sb.Append($"    add rsp, {stackBytes + pad}\n");
    }

    private int generateIdentifyer(Identifyer id, INameContainer locals, StringBuilder sb)
    {
        sb.Append($"    ; Identifyer {id.Location}\n");
        if (id.Definition is VariableDefinition varDef)
        {
            if (varDef.ValueType.Size > 8)
                throw new Exception("Variable size greater than 8 bytes not supported");

            int offset = locals.GetVariableOffset(varDef);
            string source;
            if (offset == 0)
                source = id.Definition.Name;
            else
                source = $"rbp {offset:+ 0;- 0}";

            sb.Append($"    mov rax, [{source}]\n");
        }
        else if (id.Definition is FunctionDefinition fnDef)
        {
            sb.Append($"    lea rax, [{fnDef.FullName}]\n");
        }
        else
            throw new Exception("Identifyer definition is not a variable or function");
        return 0;
    }

    private static int Align(int value, int align)
    {
        if (align <= 0) return value;
        return (value + align - 1) / align * align;
    }
}