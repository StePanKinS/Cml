using System.Runtime.CompilerServices;
using System.Text;

namespace Cml.CodeGenerating;

public class FasmCodeGen(NamespaceDefinition globalNamespace) //, ErrorReporter errorer)
{
    private NamespaceDefinition globalNamespace = globalNamespace;
    private List<string> strings = [];
    private int if_counter = 0;
    private int while_counter = 0;

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
            sb.Append($"    ; External function {fn.FullName}\n");
            sb.Append($"extrn {fn.Name}\n\n");
            return;
        }
        if (fn.Modifyers.Contains(Keywords.Export))
        {
            sb.Append($"    ; Exported function {fn.FullName}\n");
            sb.Append($"public {fn.Name}\n\n");
        }
        if (fn.Code == null)
            throw new Exception($"Function {fn.FullName} has no code");


        sb.Append($"    ; Function {fn.FullName}\n");
        sb.Append($"{fn.FullName}:\n");

        sb.Append("    push rbp\n");
        sb.Append("    mov rbp, rsp\n");

        generateExecutable(fn.Code, fn.Arguments, sb);

        if (!fn.ContainsReturn)
        {
            sb.Append("    mov rsp, rbp\n");
            sb.Append("    pop rbp\n");
            sb.Append("    ret\n");
        }
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
                sb.Append($"        ; String literal {stringLit.Location}\n");
                int count = strings.Count;
                strings.Add(stringLit.Value);
                sb.Append($"    lea rax, [str{count}]\n");
                return 0;
            case Literal<ulong> intLit:
                sb.Append($"        ; Integer literal {intLit.Location}\n");
                sb.Append($"    mov rax, {intLit.Value}\n");
                return 0;
            case Literal<bool> boolLit:
                sb.Append($"        ; Bool literal {boolLit.Location}");
                sb.Append($"    mov rax, {boolLit.Value.CompareTo(false)}");
                return 0;
            case BinaryOperation bo:
                return generateBinaryOp(bo, locals, sb);
            case Return ret:
                sb.Append($"        ; return {ret.Location}\n");
                generateExecutable(ret.Value, locals, sb);
                sb.Append($"        ; return end {ret.Location}\n");
                sb.Append($"    mov rsp, rbp\n");
                sb.Append($"    pop rbp\n");
                sb.Append($"    ret\n");
                return 0;
            case ControlFlow cf:
                int ifNum = if_counter++;
                sb.Append($"        ; if {cf.Location}\n");
                if (generateExecutable(cf.Condition, locals, sb) != 0)
                    throw new Exception("wtf bool is bool not in rax");

                sb.Append($"    test rax, rax\n");
                sb.Append($"    jz else_{ifNum}\n");
                generateExecutable(cf.Body, locals, sb);
                sb.Append($"    jmp if_end_{ifNum}\n");
                sb.Append($"else_{ifNum}:\n");
                if (cf.ElseBody != null)
                    generateExecutable(cf.ElseBody, locals, sb);

                sb.Append($"if_end_{ifNum}:\n");
                return 0;
            case WhileLoop wl:
                int whileNum = while_counter++;
                sb.Append($"        ; while loop {wl.Location}\n");
                sb.Append($"while_{whileNum}:\n");
                if (generateExecutable(wl.Condition, locals, sb) != 0)
                    throw new Exception("wtf bool is bool not in rax");
                
                sb.Append($"    test rax, rax\n");
                sb.Append($"    jz while_end_{whileNum}\n");
                generateExecutable(wl.Body, locals, sb);
                sb.Append($"    jmp while_{whileNum}\n");
                sb.Append($"while_end_{whileNum}:\n");
                return 0;
            default:
                throw new NotImplementedException($"Code generation for {exe.GetType().Name} not implemented");
        }
    }

    private static readonly Dictionary<BinaryOperationTypes, string> compareOps = new()
    {
        { BinaryOperationTypes.Less,          "l"  },
        { BinaryOperationTypes.LessEquals,    "le" },
        { BinaryOperationTypes.Greater,       "g"  },
        { BinaryOperationTypes.GreaterEquals, "ge" },
        { BinaryOperationTypes.IsEquals,      "e"  },
        { BinaryOperationTypes.NotEquals,     "ne" },
    };

    private int generateBinaryOp(BinaryOperation bo, INameContainer locals, StringBuilder sb)
    {
        switch (bo.OperationType)
        {
            case BinaryOperationTypes.Assign:
                sb.Append($"        ; Assign {bo.Location}\n");
                if (generateExecutable(bo.Right, locals, sb) != 0)
                    throw new Exception("Structs are not supported");
                if (bo.Left is Identifyer id && id.Definition is VariableDefinition variable)
                {
                    int offset = locals.GetVariableOffset(variable);
                    if (offset == 0)
                        sb.Append($"    mov [{variable.FullName}], rax\n");
                    else
                        sb.Append($"    mov [rbp {offset:+ 0;- 0}], rax\n");
                }
                return 0;
            case BinaryOperationTypes.Add:
                sb.Append($"        ; Add {bo.Location}\n");
                generateExecutable(bo.Left, locals, sb);
                sb.Append($"    push rax\n");
                generateExecutable(bo.Right, locals, sb);
                sb.Append($"    pop rbx\n");
                sb.Append($"    add rax, rbx\n");
                return 0;

            case BinaryOperationTypes.Less:
            case BinaryOperationTypes.LessEquals:
            case BinaryOperationTypes.Greater:
            case BinaryOperationTypes.GreaterEquals:
            case BinaryOperationTypes.IsEquals:
            case BinaryOperationTypes.NotEquals:
                sb.Append($"        ; Less {bo.Location}\n");
                generateExecutable(bo.Right, locals, sb);
                sb.Append($"    push rax\n");
                generateExecutable(bo.Left, locals, sb);
                sb.Append($"    pop rcx\n");
                sb.Append($"    mov rbx, rax\n");
                sb.Append($"    xor rax, rax\n");
                sb.Append($"    mov rdx, 1\n");
                sb.Append($"    sub rbx, rcx\n");
                string op = compareOps[bo.OperationType];
                sb.Append($"    cmov{op} rax, rdx\n");
                return 0;
            default:
                throw new NotImplementedException($"Binary operation {bo.OperationType} not implemented");
        }
    }

    private void generateCodeBlock(CodeBlock cb, INameContainer locals, StringBuilder sb)
    {
        if (cb.ReturnType.Size > 8)
            throw new Exception("Return type size greater than 8 bytes not supported");

        sb.Append($"        ; Code block start {cb.Location}\n");
        // ensure stack allocation is 16-byte aligned
        int alloc = Align(cb.Locals.Size + cb.ReturnType.Size, 16);
        sb.Append($"    sub rsp, {alloc}\n"); ;
        foreach (var c in cb.Code)
            generateExecutable(c, cb.Locals, sb);

        sb.Append($"        ; Code block end {cb.Location}\n");
        // sb.Append($"    pop rax\n"); // Expected code block return value on top of the stack
        sb.Append($"    add rsp, {alloc}\n");
    }

    private void generateFunctioncall(FunctionCall fc, INameContainer locals, StringBuilder sb)
    {
        sb.Append($"        ; Function call {fc.Location}\n");
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
        sb.Append($"        ; Identifyer {id.Location}\n");
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