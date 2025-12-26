using System.Text;

namespace Cml.CodeGenerating;

public class FasmCodeGen(NamespaceDefinition globalNamespace) //, ErrorReporter errorer)
{
    private NamespaceDefinition globalNamespace = globalNamespace;
    private List<string> stringLiterals = [];
    private List<double> doubleLiterals = [];
    private int if_counter = 0;
    private int while_counter = 0;


    private static readonly string[] CallRegisters = ["rdi", "rsi", "rdx", "rcx", "r8", "r9"];

    private static readonly Dictionary<BinaryOperationTypes, string> CompareOps = new()
    {
        { BinaryOperationTypes.Less,          "l"  },
        { BinaryOperationTypes.LessEquals,    "le" },
        { BinaryOperationTypes.Greater,       "g"  },
        { BinaryOperationTypes.GreaterEquals, "ge" },
        { BinaryOperationTypes.IsEquals,      "e"  },
        { BinaryOperationTypes.NotEquals,     "ne" },
    };

    private static readonly Dictionary<BinaryOperationTypes, string> MathOps = new()
    {
        { BinaryOperationTypes.Add,        "add"  },
        { BinaryOperationTypes.Subtract,   "sub"  },
        { BinaryOperationTypes.Multiply,   "imul" }, // TODO: umul ?
        { BinaryOperationTypes.BitwiseAnd, "and"  },
        { BinaryOperationTypes.BitwiseOr,  "or"   },
        { BinaryOperationTypes.BitwiseXor, "xor"  },
        { BinaryOperationTypes.LeftShift,  "shl"  },
        { BinaryOperationTypes.RightShift, "shr"  },
    };

    private static readonly Dictionary<BinaryOperationTypes, string> FloatMathOps = new()
    {
        { BinaryOperationTypes.Add,        "addsd"  },
        { BinaryOperationTypes.Subtract,   "subsd"  },
        { BinaryOperationTypes.Multiply,   "mulsd" },
    };

    private static readonly Dictionary<int, string> RegisterSizes = new()
    {
        { 1, "al"  },
        { 2, "ax"  },
        { 4, "eax" },
        { 8, "rax" },
    };

    private static readonly Dictionary<int, string> MemorySizeNames = new()
    {
        { 1, "byte"  },
        { 2, "word"  },
        { 4, "dword" },
        { 8, "qword" },
    };



    public string Generate()
        => topLevel();

    private string topLevel()
    {
        StringBuilder sb = new();
        sb.Append("format ELF64\n\n");
        sb.Append("section '.text' executable writeable\n");

        generateNamespace(globalNamespace, sb);

        sb.Append("section '.data' writeable\n");
        sb.Append($"bit63 dq 1 shl 63\n");
        generateStrings(sb);
        generateDoubles(sb);


        return sb.ToString();
    }

    private void generateDoubles(StringBuilder sb)
    {
        for (int i = 0; i < doubleLiterals.Count; i++)
        {
            sb.Append($"double{i} dq {doubleLiterals[i]}\n");
        }
    }

    private void generateStrings(StringBuilder sb)
    {
        for (int i = 0; i < stringLiterals.Count; i++)
        {
            string s = stringLiterals[i];
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
            sb.Append($"public {fn.FullName}\n");
        }
        if (fn.Code == null)
            throw new Exception($"Function {fn.FullName} has no code");


        sb.Append($"    ; Function {fn.FullName}\n");
        sb.Append($"{fn.FullName}:\n");

        sb.Append("    push rbp\n");
        sb.Append("    mov rbp, rsp\n");

        var (intCount, floatCount, _) = fn.Arguments.GetClassCount();
        int ic = 0;
        int fc = 0;

        for (int i = 0; ic != intCount || fc != floatCount; i++)
        {
            Typ itype = fn.Arguments.Arguments[i].Type;
            if ((itype is DefaultType.Integer || itype is Pointer)
                && ic < intCount)
                sb.Append($"    push {CallRegisters[ic++]}\n");
            else if (itype is DefaultType.FloatingPoint fp
                && fc < floatCount)
            {
                sb.Append($"    sub rsp, 8\n");
                if (fp.Size == 4)
                    sb.Append($"    movss [rsp], xmm{fc++}\n");
                else if (fp.Size == 8)
                    sb.Append($"    movsd [rsp], xmm{fc++}\n");
                else
                    throw new Exception($"Unsupported float size {fp.Size} in genFunc");
            }
            else
                throw new Exception($"Unsupported type {itype} in genFunc");
        }

        generateExecutable(fn.Code, fn.Arguments, sb);

        if (!fn.ContainsReturn)
        {
            sb.Append("    mov rsp, rbp\n");
            sb.Append("    pop rbp\n");
            sb.Append("    ret\n");
        }
        sb.Append('\n');
    }

    private void generateExecutable(Executable exe, INameContainer locals, StringBuilder sb)
    {
        switch (exe)
        {
            case CodeBlock cb:
                generateCodeBlock(cb, locals, sb);
                return;

            case FunctionCall fc:
                generateFunctionCall(fc, locals, sb);
                return;

            case Identifyer id:
                generateIdentifyer(id, locals, sb);
                return;

            case Literal<string> stringLit:
                sb.Append($"        ; String literal {stringLit.Location}\n");
                int scount = stringLiterals.Count;
                stringLiterals.Add(stringLit.Value);
                sb.Append($"    lea rax, [str{scount}]\n");
                return;

            case Literal<ulong> intLit:
                sb.Append($"        ; Integer literal {intLit.Location}\n");
                sb.Append($"    mov rax, {intLit.Value}\n");
                return;

            case Literal<bool> boolLit:
                sb.Append($"        ; Bool literal {boolLit.Location}\n");
                sb.Append($"    mov rax, {boolLit.Value.CompareTo(false)}\n");
                return;

            case Literal<double> doubleLit:
                sb.Append($"        ; Double literal {doubleLit.Location}\n");
                int dcount = doubleLiterals.Count;
                doubleLiterals.Add(doubleLit.Value);
                sb.Append($"    movsd xmm0, [double{dcount}]\n");
                return;


            case BinaryOperation bo:
                generateBinaryOp(bo, locals, sb);
                return;

            case UnaryOperation uo:
                generateUnaryOP(uo, locals, sb);
                return;

            case Return ret:
                sb.Append($"        ; return {ret.Location}\n");
                generateExecutable(ret.Value, locals, sb);
                sb.Append($"    mov rsp, rbp\n");
                sb.Append($"    pop rbp\n");
                sb.Append($"    ret\n");
                return;

            case ControlFlow cf:
                int ifNum = if_counter++;
                sb.Append($"        ; if {cf.Location}\n");
                generateExecutable(cf.Condition, locals, sb);

                sb.Append($"    test rax, rax\n");
                sb.Append($"    jz else_{ifNum}\n");
                generateExecutable(cf.Body, locals, sb);
                sb.Append($"    jmp if_end_{ifNum}\n");
                sb.Append($"else_{ifNum}:\n");
                if (cf.ElseBody != null)
                    generateExecutable(cf.ElseBody, locals, sb);

                sb.Append($"if_end_{ifNum}:\n");
                return;

            case WhileLoop wl:
                int whileNum = while_counter++;
                sb.Append($"        ; while loop {wl.Location}\n");
                sb.Append($"while_{whileNum}:\n");
                generateExecutable(wl.Condition, locals, sb);

                sb.Append($"    test rax, rax\n");
                sb.Append($"    jz while_end_{whileNum}\n");
                generateExecutable(wl.Body, locals, sb);
                sb.Append($"    jmp while_{whileNum}\n");
                sb.Append($"while_end_{whileNum}:\n");
                return;

            case GetMember gm:
                generateGetMember(gm, locals, sb);
                return;

            case GetElement ge:
                generateGetElement(ge, locals, sb);
                return;

            default:
                throw new NotImplementedException($"Code generation for {exe.GetType().Name} not implemented");
        }
    }

    private void generateGetElementAddress(GetElement ge, INameContainer locals, StringBuilder sb)
    {
        generateExecutable(ge.Index, locals, sb);
        sb.AppendLine($"    push rax");
        generateExecutable(ge.Operand, locals, sb);
        sb.AppendLine($"    pop rbx");
        int elsize;
        if (ge.Operand.ReturnType is SizedArray sa)
            elsize = sa.ElementType.Size;
        else if (ge.Operand.ReturnType is Pointer p)
            elsize = p.PointsTo.Size;
        else
            throw new Exception($"wtf did parser put into ge.operand {ge.Operand}. in generateGetElementAddress");

        sb.AppendLine($"    imul rbx, {elsize}");
        sb.AppendLine($"    add rax, rbx");
    }

    private void generateGetElement(GetElement ge, INameContainer locals, StringBuilder sb)
    {
        sb.AppendLine($"        ; GetElement {ge.Location}");
        generateGetElementAddress(ge, locals, sb);
        sb.AppendLine($"    mov rbx, rax");
        loadValue("rbx", ge.ReturnType, locals, sb);
    }

    private void generateGetMember(GetMember gm, INameContainer locals, StringBuilder sb)
    {
        sb.Append($"        ; GetMember {gm.Location}\n");
        generateExecutable(gm.Operand, locals, sb);
        if (gm.Operand.ReturnType is not StructType st)
            throw new Exception($"Expected composite type, not {gm.Operand.ReturnType}. In genGetMember");

        int offset = st.GetMemberOffset(gm.Member.Value);
        var targetType = st.GetStructMember(gm.Member.Value)!.Type;
        loadValue($"rax + {offset}", targetType, locals, sb);
    }

    private Definition getGMdefinition(GetMember gm)
    {
        NamespaceDefinition inmsp;
        if (gm.Operand is Identifyer ident)
        {
            if (ident.Definition is not NamespaceDefinition nmsp1)
                throw new Exception($"Unexpected definition {ident.Definition.GetType().Name}, namespace expected");

            inmsp = nmsp1;
        }
        else if (gm.Operand is GetMember igm)
        {
            var definition = getGMdefinition(igm);
            if (definition is not NamespaceDefinition nmsp2)
                throw new Exception($"Unexpected definition {definition.GetType().Name}, namespace expected");

            inmsp = nmsp2;
        }
        else
            throw new Exception("How did i get here. in getGMnmsp");

        if (!inmsp.TryGetName(gm.Member.Value, out var def))
            throw new Exception($"Member {gm.Member.Value} does not exist in {inmsp}");
        return def;
    }

    private void generateBinaryOp(BinaryOperation bo, INameContainer locals, StringBuilder sb)
    {
        switch (bo.OperationType)
        {
            case BinaryOperationTypes.Assign:
                sb.Append($"        ; Assign {bo.Location}\n");
                generateExecutable(bo.Right, locals, sb);
                string target;

                if (bo.Left is Identifyer id && id.Definition is VariableDefinition variable)
                {
                    int offset = locals.GetVariableOffset(variable);
                    target = offset == 0 ? variable.FullName : $"rbp {offset:+ 0;- 0}";
                }
                else if (bo.Left is GetMember gm)
                {
                    sb.Append($"    push rax\n");
                    generateExecutable(gm.Operand, locals, sb);
                    if (gm.Operand.ReturnType is not StructType st)
                        throw new Exception($"Expected composite type, not {gm.Operand.ReturnType}. In genBinary");

                    int offset = st.GetMemberOffset(gm.Member.Value);
                    sb.Append($"    mov rbx, rax\n");
                    sb.Append($"    add rbx, {offset}\n");
                    sb.Append($"    pop rax\n");
                    target = "rbx";
                }
                else if (bo.Left is GetElement ge)
                {
                    sb.AppendLine($"    push rax");
                    generateGetElementAddress(ge, locals, sb);
                    sb.AppendLine($"    mov rbx, rax");
                    sb.AppendLine($"    pop rax");
                    target = "rbx";
                }
                else
                    throw new Exception($"Unsupported lhs {bo.Left} in assign");

                if (bo.Right.ReturnType is DefaultType.Integer
                    || bo.Right.ReturnType is Pointer)
                    sb.Append($"    mov [{target}], {RegisterSizes[bo.Right.ReturnType.Size]}\n");
                else if (bo.Right.ReturnType is DefaultType.FloatingPoint fp)
                {
                    if (fp.Size == 4)
                    {
                        sb.Append($"    cvtsd2ss xmm0, xmm0\n");
                        sb.Append($"    movss [{target}], xmm0\n");
                    }
                    else if (fp.Size == 8)
                        sb.Append($"    movsd [{target}], xmm0\n");
                    else
                        throw new Exception($"Unsupported float size {fp.Size} in assign");
                }
                else
                {
                    sb.Append($"    lea rdi, [{target}]\n");
                    sb.Append($"    mov rsi, rax\n");
                    sb.Append($"    mov rcx, {bo.Left.ReturnType.Size}\n");
                    sb.Append($"    rep movsb\n");
                    sb.Append($"    mov rax, rdi\n");
                }

                return;

            case BinaryOperationTypes.Add:
            case BinaryOperationTypes.Subtract:
            case BinaryOperationTypes.Multiply:
            case BinaryOperationTypes.BitwiseAnd:
            case BinaryOperationTypes.BitwiseOr:
            case BinaryOperationTypes.BitwiseXor:
                sb.Append($"        ; {bo.OperationType} {bo.Location}\n");
                if (bo.ReturnType is DefaultType.Integer)
                {
                    generateExecutable(bo.Right, locals, sb);
                    sb.Append($"    push rax\n");
                    generateExecutable(bo.Left, locals, sb);
                    sb.Append($"    pop rbx\n");
                    sb.Append($"    {MathOps[bo.OperationType]} rax, rbx\n");
                }
                else if (bo.ReturnType is DefaultType.FloatingPoint)
                {
                    generateExecutable(bo.Right, locals, sb);
                    sb.Append($"    sub rsp, 8\n");
                    sb.Append($"    movsd [rsp], xmm0\n");
                    generateExecutable(bo.Left, locals, sb);
                    sb.Append($"    movsd xmm1, [rsp]\n");
                    sb.Append($"    add rsp, 8\n");
                    sb.Append($"    {FloatMathOps[bo.OperationType]} xmm0, xmm1\n");
                }
                else
                    throw new Exception($"Unsupported type {bo.ReturnType} in genBinary");

                return;

            case BinaryOperationTypes.Less:
            case BinaryOperationTypes.LessEquals:
            case BinaryOperationTypes.Greater:
            case BinaryOperationTypes.GreaterEquals:
            case BinaryOperationTypes.IsEquals:
            case BinaryOperationTypes.NotEquals:
                sb.Append($"        ; {bo.OperationType} {bo.Location}\n");

                string compareOperation;

                if (bo.Left.ReturnType is DefaultType.Integer)
                {
                    generateExecutable(bo.Right, locals, sb);
                    sb.Append($"    push rax\n");
                    generateExecutable(bo.Left, locals, sb);
                    sb.Append($"    pop rcx\n");
                    sb.Append($"    mov rbx, rax\n");
                    compareOperation = $"    sub rbx, rcx\n";
                }
                else if (bo.Left.ReturnType is DefaultType.FloatingPoint)
                {
                    generateExecutable(bo.Right, locals, sb);
                    sb.Append($"    sub rsp, 8\n");
                    sb.Append($"    movsd [rsp], xmm0\n");
                    generateExecutable(bo.Left, locals, sb);
                    sb.Append($"    movsd xmm1, [rsp]\n");
                    sb.Append($"    add rsp, 8\n");
                    compareOperation = $"    comisd xmm1, xmm0\n";
                }
                else
                    throw new Exception($"Unsupported type {bo.ReturnType} in genBinary");

                sb.Append($"    xor rax, rax\n");
                sb.Append($"    mov rdx, 1\n");
                sb.Append(compareOperation);
                string op = CompareOps[bo.OperationType];
                sb.Append($"    cmov{op} rax, rdx\n");
                return;

            case BinaryOperationTypes.Divide:
                sb.Append($"        ; Division {bo.Location}\n");
                if (bo.ReturnType is DefaultType.Integer)
                {
                    generateExecutable(bo.Right, locals, sb);
                    sb.Append($"    push rax\n");
                    generateExecutable(bo.Left, locals, sb);
                    sb.Append($"    pop rbx\n");
                    sb.Append($"    xor rdx, rdx\n");
                    sb.Append($"    idiv rbx\n");
                }
                else if (bo.ReturnType is DefaultType.FloatingPoint)
                {
                    generateExecutable(bo.Right, locals, sb);
                    sb.Append($"    sub rsp, 8\n");
                    sb.Append($"    movsd [rsp], xmm0\n");
                    generateExecutable(bo.Left, locals, sb);
                    sb.Append($"    movsd xmm1, [rsp]\n");
                    sb.Append($"    add rsp, 8\n");
                    sb.Append($"    divsd xmm0, xmm1\n");
                }
                else
                    throw new Exception($"Unsupported type {bo.ReturnType} in genBinary");
                return;

            case BinaryOperationTypes.Remainder:
                sb.Append($"        ; Remainder {bo.Location}\n");
                generateExecutable(bo.Right, locals, sb);
                sb.Append($"    push rax\n");
                generateExecutable(bo.Left, locals, sb);
                sb.Append($"    pop rbx\n");
                sb.Append($"    xor rdx, rdx\n");
                sb.Append($"    idiv rbx\n");
                sb.Append($"    mov rax, rdx\n");
                return;

            default:
                throw new NotImplementedException($"Binary operation {bo.OperationType} not implemented");
        }
    }

    private void generateUnaryOP(UnaryOperation uo, INameContainer locals, StringBuilder sb)
    {
        switch (uo.OperationType)
        {
            case UnaryOperationTypes.Cast:
                sb.Append($"        ; Cast {uo.Location}\n");
                generateCsat(uo.Operand, uo.ReturnType, locals, sb);
                return;

            case UnaryOperationTypes.GetReference:
                if (uo.Operand is Identifyer ident
                    && ident.Definition is VariableDefinition variable)
                {
                    sb.Append($"        ; GetReference {uo.Location}\n");
                    int offset = locals.GetVariableOffset(variable);
                    if (offset == 0)
                        sb.Append($"    lea rax, [{variable.FullName}]\n");
                    else
                        sb.Append($"    lea rax, [rbp {offset:+ 0;- 0}]\n");
                }
                else if (uo.Operand is GetElement ge)
                    generateGetElementAddress(ge, locals, sb);
                else
                    throw new Exception($"Cant get ref from {uo.Operand}. genUO.GerRef");

                return;

            case UnaryOperationTypes.Minus:
                sb.Append($"        ; Unary minus {uo.Location}\n");
                generateExecutable(uo.Operand, locals, sb);

                if (uo.Operand.ReturnType is DefaultType.Integer)
                    sb.Append($"    neg rax\n");
                else if (uo.Operand.ReturnType is DefaultType.FloatingPoint)
                    sb.Append($"    xorsd xmm0, [bit63]\n");
                else
                    throw new Exception($"Unknown type {uo.Operand.ReturnType} in unary minus");

                return;

            case UnaryOperationTypes.Dereference:
                sb.Append($"        ; Unary operation dereference {uo.Location}\n");
                generateExecutable(uo.Operand, locals, sb);
                loadValue("rax", uo.ReturnType, locals, sb);
                return;

            default:
                throw new NotImplementedException($"Unary operation {uo.OperationType}");
        }
    }

    private void generateCsat(Executable operand, Typ target, INameContainer locals, StringBuilder sb)
    {
        generateExecutable(operand, locals, sb);
        if (operand.ReturnType is Pointer || target is Pointer)
            return;
        else if (target is DefaultType.Integer to
            && operand.ReturnType is DefaultType.Integer from)
        {
            if (from.Size >= to.Size || from.Size == 0)
                return; // No action needed for truncation
            if (from.IsSigned && !to.IsSigned)
            {
                if (to.Size == 8 && from.Size == 4)
                    sb.Append($"    mov eax, eax\n"); // movzx r64, r/m32 does not exist
                else
                    sb.Append($"    movzx {RegisterSizes[to.Size]}, {RegisterSizes[from.Size]}\n");
            }
        }
        else if (operand.ReturnType is DefaultType.FloatingPoint ff
            && target is DefaultType.FloatingPoint ft)
            return;
        else if (operand.ReturnType is DefaultType.Integer
            && target is DefaultType.FloatingPoint)
            sb.Append($"    cvtsi2sd xmm0, rax\n"); // TODO: 64bit unsigned is ignored
        else if (operand.ReturnType is DefaultType.FloatingPoint
            && target is DefaultType.Integer)
            sb.Append($"    cvtsd2si rax, xmm0\n");
        else
            throw new NotImplementedException("Only integer, pointer ans float casts are implemented");

        return;
    }

    private void generateCodeBlock(CodeBlock cb, INameContainer locals, StringBuilder sb)
    {
        sb.Append($"        ; Code block start {cb.Location}\n");

        if (cb.Locals.SelfSize != 0)
            sb.Append($"    sub rsp, {cb.Locals.SelfSize}\n");

        foreach (var c in cb.Code)
            generateExecutable(c, cb.Locals, sb);

        if (cb.Locals.SelfSize != 0)
        {
            sb.Append($"        ; Code block end {cb.Location}\n");
            sb.Append($"    add rsp, {cb.Locals.SelfSize}\n");
        }
    }

    private void generateFunctionCall(FunctionCall fc, INameContainer locals, StringBuilder sb)
    {
        sb.Append($"        ; Function call {fc.Location}\n");

        // SysV AMD64 calling convention: 
        // first 6 integer/pointer args in registers (rdi,rsi,rdx,rcx,r8,r9),
        // first 8 float in regs xmm0-xmm7,
        // remaining args pushed on the stack.
        // regs args order is first arg in first reg,
        // on stack last argument is on higher memory address
        // stack should be 16-byte alligned, padding is added if needed
        // as it would be the last argument


        var (intCount, floatCount, stackCount) = FunctionArguments.GetClassCount(fc.Args.Select(i => i.ReturnType));


        bool pad = stackCount % 2 == 1;
        if (pad)
            sb.Append($"    sub rsp, 8\n");

        // push all args on stack to prevent overriding 
        // thier value during evaluetion of other args
        for (int i = fc.Args.Length - 1; i >= 0; i--)
        {
            generateExecutable(fc.Args[i], locals, sb);

            if (fc.Args[i].ReturnType is DefaultType.Integer
                || fc.Args[i].ReturnType is Pointer)
                sb.Append($"    push rax\n");
            else if (fc.Args[i].ReturnType is DefaultType.FloatingPoint fp)
            {
                sb.Append($"    sub rsp, 8\n");
                sb.Append($"    movsd [rsp], xmm0\n");
            }
            else
                throw new Exception($"Unknown type {fc.Args[i].ReturnType} in generateFunctionCall");
        }

        // pop back args that should be in rags
        int ic = 0;
        int flc = 0;
        for (int i = 0; ic != intCount || flc != floatCount; i++)
        {
            if (fc.Args[i].ReturnType is DefaultType.Integer
                || fc.Args[i].ReturnType is Pointer)
                sb.Append($"    pop {CallRegisters[ic++]}\n");
            else if (fc.Args[i].ReturnType is DefaultType.FloatingPoint fp)
            {
                sb.Append($"    movsd xmm{flc}, [rsp]\n");
                sb.Append($"    add rsp, 8\n");

                if (fp.Size == 4)
                    sb.Append($"    cvtsd2ss xmm{flc}, xmm{flc}\n");
                else if (fp.Size != 8)
                    throw new Exception($"Unknown float size {fp.Size} in genFuncCall");

                flc++;
            }
            else
                throw new Exception($"Unsupported type {fc.Args[i].ReturnType} in genFuncCall");
        }

        generateExecutable(fc.FunctionPointer, locals, sb);
        sb.Append($"    mov rbx, rax\n");
        sb.Append($"    mov rax, {floatCount}\n");
        sb.Append($"    call rbx\n");

        // clean up pushed stack arguments and any padding
        int stackSize = (stackCount + (pad ? 1 : 0)) * 8;
        if (stackSize > 0)
            sb.Append($"    add rsp, {stackSize}\n");
    }

    private void generateIdentifyer(Identifyer id, INameContainer locals, StringBuilder sb)
    {
        sb.Append($"        ; Identifyer {id.Location}\n");
        if (id.Definition is VariableDefinition varDef)
        {
            int offset = locals.GetVariableOffset(varDef);
            string source;
            if (offset == 0)
                source = id.Definition.Name;
            else
                source = $"rbp {offset:+ 0;- 0}";

            loadValue(source, varDef.Type, locals, sb);
        }
        else if (id.Definition is FunctionDefinition fnDef)
        {
            string name = fnDef.Modifyers.Contains(Keywords.External) ? fnDef.Name : fnDef.FullName;
            sb.Append($"    lea rax, [{name}]\n");
        }
        else
            throw new Exception("Identifyer is not a variable or function");
    }

    // do not pass value ptr in rax...
    private void loadValue(string source, Typ valueType, INameContainer locals, StringBuilder sb)
    {
        if (valueType is Pointer)
            sb.Append($"    mov rax, [{source}]\n");
        else if (valueType is DefaultType.Integer it)
        {
            if (it.Size == 8)
                sb.Append($"    mov rax, [{source}]\n");
            else
            {
                if (!it.IsSigned)
                {
                    sb.Append($"    xor rax, rax\n");
                    sb.Append($"    mov {RegisterSizes[valueType.Size]}, [{source}]\n");
                }
                else
                    sb.Append($"    movsx{(it.Size == 4 ? 'd' : "")} rax, {MemorySizeNames[it.Size]} [{source}]\n");
            }
        }
        else if (valueType is DefaultType.FloatingPoint fp)
        {
            if (fp.Size == 4)
            {
                sb.Append($"    movss xmm0, [{source}]\n");
                sb.Append($"    cvtss2sd xmm0, xmm0\n");
            }
            else if (fp.Size == 8)
                sb.Append($"    movsd xmm0, [{source}]\n");
            else
                throw new Exception($"Unknown float size {fp.Size} in genIdent");
        }
        else
            sb.Append($"    lea rax, [{source}]\n");
    }
}
