using System.Text;

namespace Cml.CodeGeneration;

public class LlvmCodeGen(IEnumerable<FileDefinition> files)
{
    private IEnumerable<FileDefinition> Files = files;
    private List<string> stringLiterals = [];
    private List<double> doubleLiterals = [];
    private int ifsCounter = 0;
    private int loopsCounter = 0;
    private int valueCounter = 0;
    private Dictionary<Loop, int> loopNumbers = [];

    private static readonly Dictionary<BinaryOperationTypes, string> CompareOps = new()
    {
        { BinaryOperationTypes.Less,          "slt"  },
        { BinaryOperationTypes.LessEquals,    "sle"  },
        { BinaryOperationTypes.Greater,       "sgt"  },
        { BinaryOperationTypes.GreaterEquals, "sge"  },
        { BinaryOperationTypes.IsEquals,      "eq"   },
        { BinaryOperationTypes.NotEquals,     "ne"   },
    };

    private static readonly Dictionary<BinaryOperationTypes, string> MathOps = new()
    {
        { BinaryOperationTypes.Add,        "add"  },
        { BinaryOperationTypes.Subtract,   "sub"  },
        { BinaryOperationTypes.Multiply,   "mul"  },
        { BinaryOperationTypes.BitwiseAnd, "and"  },
        { BinaryOperationTypes.BitwiseOr,  "or"   },
        { BinaryOperationTypes.BitwiseXor, "xor"  },
        { BinaryOperationTypes.LeftShift,  "shl"  },
        { BinaryOperationTypes.RightShift, "lshr" },
    };

    private static readonly Dictionary<BinaryOperationTypes, string> FloatMathOps = new()
    {
        { BinaryOperationTypes.Add,      "fadd" },
        { BinaryOperationTypes.Subtract, "fsub" },
        { BinaryOperationTypes.Multiply, "fmul" },
        { BinaryOperationTypes.Divide,   "fdiv" },
    };

    private (string startLabel, string endLabel) getLoopLabels(Loop loop)
    {
        int num;
        if (!loopNumbers.TryGetValue(loop, out num))
        {
            num = loopsCounter++;
            loopNumbers[loop] = num;
        }
        return ($"loop_start_{num}", $"loop_end_{num}");
    }

    private string getNextValueName()
    {
        return $"%{valueCounter++}";
    }

    private string getLlvmType(Typ type)
    {
        if (type is DefaultType.Integer intType)
            return intType.Size switch
            {
                1 => "i8",
                2 => "i16",
                4 => "i32",
                8 => "i64",
                0 => "i64",
                _ => throw new Exception($"Unknown int size {intType.Size}"),
            };
        else if (type is DefaultType.FloatingPoint fpType)
            return fpType.Size switch
            {
                4 => "float",
                8 => "double",
                _ => throw new Exception($"Unknown float size {fpType.Size}"),
            };
        else if (type == DefaultType.Bool)
            return "i1";
        else if (type == DefaultType.Char)
            return "i8";
        else if (type == DefaultType.Void)
            return "void";
        else if (type is Pointer ptr)
            return $"ptr";
        // return $"{getLlvmType(ptr.PointsTo)}*";
        else if (type is SizedArray arr)
            return $"[{arr.ElementCount} x {getLlvmType(arr.ElementType)}]";
        else if (type is StructType st)
            return $"%struct.{st.Name}";
        else if (type is EnumType enumType)
            return "i64"; // Enums are 64-bit integers
        else
            throw new Exception($"Unknown type {type}");
    }

    public string Generate()
        => topLevel();

    private string topLevel()
    {
        StringBuilder sb = new();
        sb.AppendLine("; LLVM IR generated from CML");
        sb.AppendLine("target triple = \"x86_64-unknown-linux-gnu\"");
        sb.AppendLine("target datalayout = \"e-m:e-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128\"");
        sb.AppendLine();

        generateStructDefinitions(sb);
        sb.AppendLine();

        foreach (var file in Files)
            generateNamespace(file, sb);

        generateStringGlobals(sb);
        sb.AppendLine();

        generateDoubleGlobals(sb);
        sb.AppendLine();

        return sb.ToString();
    }

    private void generateStructDefinitions(StringBuilder sb)
    {
        var structTypes = new HashSet<StructType>();

        // Collect all struct types from all definitions
        foreach (var file in Files)
            collectStructTypesFromDefinition(file, structTypes);

        foreach (var structType in structTypes)
        {
            var members = string.Join(", ",
                structType.Members.Select(m => getLlvmType(m.Type)));
            sb.AppendLine($"%struct.{structType.Name} = type {{ {members} }}");
        }
    }

    private void collectStructTypesFromDefinition(Definition def, HashSet<StructType> structs)
    {
        if (def is NamespaceDefinition nmsp)
        {
            foreach (var subdef in nmsp)
                collectStructTypesFromDefinition(subdef, structs);
        }
        else if (def is StructDefinition sd)
        {
            if (!structs.Contains(sd.StructType))
            {
                structs.Add(sd.StructType);
                foreach (var member in sd.StructType.Members)
                    collectStructTypes(structs, member.Type);
            }
        }
        else if (def is FunctionDefinition fn)
        {
            if (fn.ReturnType != DefaultType.Void)
                collectStructTypes(structs, fn.ReturnType);
            foreach (var arg in fn.Arguments.Arguments)
                collectStructTypes(structs, arg.Type);
        }
    }

    private void collectStructTypes(HashSet<StructType> structs, Typ type)
    {
        if (type is StructType st && !structs.Contains(st))
        {
            structs.Add(st);
            foreach (var member in st.Members)
                collectStructTypes(structs, member.Type);
        }
        else if (type is SizedArray arr)
            collectStructTypes(structs, arr.ElementType);
        else if (type is Pointer ptr)
            collectStructTypes(structs, ptr.PointsTo);
    }

    private void generateStringGlobals(StringBuilder sb)
    {
        for (int i = 0; i < stringLiterals.Count; i++)
        {
            string escapedString = stringLiterals[i]
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\0A")
                .Replace("\r", "\\0D")
                .Replace("\t", "\\09");

            sb.AppendLine($"@str{i} = private unnamed_addr constant [{stringLiterals[i].Length + 1} x i8] c\"{escapedString}\\00\"");
        }
    }

    private void generateDoubleGlobals(StringBuilder sb)
    {
        for (int i = 0; i < doubleLiterals.Count; i++)
        {
            sb.AppendLine($"@double{i} = private unnamed_addr constant double {doubleLiterals[i]:G17}");
        }
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

                case DefaultTypeDefinition:
                case StructDefinition: // Struct definitions are generated earlier
                    break;

                default:
                    throw new Exception($"Unknown def: {def}");
            }
        }
    }

    private void generateFunction(FunctionDefinition fn, StringBuilder sb)
    {
        if (fn.Modifyers.Contains(Keywords.External))
        {
            // External function - just declare it
            string returnType = getLlvmType(fn.ReturnType);
            var paramTypes = string.Join(", ", fn.Arguments.Arguments.Select(arg => getLlvmType(arg.Type)));
            sb.AppendLine($"declare {returnType} @{fn.Name}({paramTypes})");
            // sb.AppendLine($"@{fn.FullName} = alias {returnType}({paramTypes}), ptr @{fn.Name}");
            sb.AppendLine();
            return;
        }

        string retType = getLlvmType(fn.ReturnType);
        var paramList = string.Join(", ", fn.Arguments.Arguments.Select((arg, idx) => $"{getLlvmType(arg.Type)} %arg{idx}"));

        string linkage = fn.Modifyers.Contains(Keywords.Export) ? "" : "internal ";
        sb.AppendLine($"define {linkage}{retType} @{fn.FullName}({paramList}) {{");
        sb.AppendLine("entry:");

        // Allocate space for arguments and local variables
        int argIdx = 0;
        foreach (var arg in fn.Arguments.Arguments)
        {
            string allocName = $"%{arg.FullName}";
            sb.AppendLine($"    {allocName} = alloca {getLlvmType(arg.Type)}");
            sb.AppendLine($"    store {getLlvmType(arg.Type)} %arg{argIdx}, ptr {allocName}");
            argIdx++;
        }

        // Generate function body
        if (fn.Code == null)
            throw new Exception($"null code {fn}");
        generateExecutable(fn.Code, fn.Arguments, sb);

        // Add implicit return if needed
        if (!fn.ContainsReturn)
        {
            if (fn.ReturnType != DefaultType.Void)
                throw new Exception($"no return in non void func {fn}");

            sb.AppendLine("    ret void");
        }

        sb.AppendLine("}");
        sb.AppendLine();
    }

    private string generateExecutable(Executable exe, INameContainer locals, StringBuilder sb)
    {
        switch (exe)
        {
            case CodeBlock cb:
                return generateCodeBlock(cb, locals, sb);

            case FunctionCall fc:
                return generateFunctionCall(fc, locals, sb);

            case MethodCall mc:
                return generateMethodCall(mc, locals, sb);

            case MethodValue mv:
                return generateMethodValue(mv, locals, sb);

            case Identifyer id:
                return generateIdentifier(id, locals, sb);

            case Literal<string> stringLit:
                {
                    int scount = stringLiterals.Count;
                    stringLiterals.Add(stringLit.Value);
                    string result = getNextValueName();
                    sb.AppendLine($"        ; String {stringLit.Location}");
                    sb.AppendLine($"    {result} = getelementptr [{stringLit.Value.Length + 1} x i8], [{stringLit.Value.Length + 1} x i8]* @str{scount}, i64 0, i64 0");
                    return result;
                }

            case Literal<ulong> intLit:
                {
                    sb.AppendLine($"        ; Integer literal {intLit.Location}");
                    // return $"{getLlvmType(intLit.ReturnType)} {intLit.Value}";
                    return $"{intLit.Value}";
                }

            case Literal<bool> boolLit:
                {
                    sb.AppendLine($"        ; Bool literal {boolLit.Location}");
                    // return $"i1 {(boolLit.Value ? "1" : "0")}";
                    return boolLit.Value ? "1" : "0";
                }

            case Literal<double> doubleLit:
                {
                    int dcount = doubleLiterals.Count;
                    doubleLiterals.Add(doubleLit.Value);
                    string result = getNextValueName();
                    sb.AppendLine($"    {result} = load double, ptr @double{dcount} ; Literal {doubleLit.Location}");
                    return result;
                }

            case BinaryOperation bo:
                return generateBinaryOp(bo, locals, sb);

            case UnaryOperation uo:
                return generateUnaryOp(uo, locals, sb);

            case Return ret:
                {
                    string retVal = generateExecutable(ret.Value, locals, sb);
                    sb.AppendLine($"        ; return {ret.Location}");
                    if (ret.Value.ReturnType == DefaultType.Void)
                        sb.AppendLine("    ret void");
                    else
                        sb.AppendLine($"    ret {getLlvmType(ret.Value.ReturnType)} {retVal}");
                    return ""; // Not used after return
                }

            case ControlFlow cf:
                {
                    int ifNum = ifsCounter++;
                    sb.AppendLine($"    ; if {cf.Location}");
                    string condValue = generateExecutable(cf.Condition, locals, sb);
                    string testVal = getNextValueName();
                    sb.AppendLine($"    {testVal} = icmp ne i1 {condValue}, 0");
                    sb.AppendLine($"    br i1 {testVal}, label %if_then_{ifNum}, label %if_else_{ifNum}");

                    sb.AppendLine($"if_then_{ifNum}:");
                    generateExecutable(cf.Body, locals, sb);
                    sb.AppendLine($"    br label %if_end_{ifNum}");

                    sb.AppendLine($"if_else_{ifNum}:");
                    if (cf.ElseBody != null)
                        generateExecutable(cf.ElseBody, locals, sb);
                    sb.AppendLine($"    br label %if_end_{ifNum}");

                    sb.AppendLine($"if_end_{ifNum}:");
                    return "";
                }

            case WhileLoop wl:
                {
                    var (sl, el) = getLoopLabels(wl);
                    sb.AppendLine($"    ; while loop {wl.Location}");
                    sb.AppendLine($"    br label %{sl}");
                    sb.AppendLine($"{sl}:");
                    string condValue = generateExecutable(wl.Condition, locals, sb);
                    sb.AppendLine($"    br i1 {condValue}, label %{sl}_body, label %{el}");
                    sb.AppendLine($"{sl}_body:");
                    generateExecutable(wl.Body, locals, sb);
                    sb.AppendLine($"    br label %{sl}");
                    sb.AppendLine($"{el}:");
                    return "";
                }

            case GetMember gm:
                return generateGetMember(gm, locals, sb);

            case GetElement ge:
                return generateGetElement(ge, locals, sb);

            case PostIncrement pi:
                return generateIncrement(pi.Operand, pi.IsDecrement, true, pi.Location, locals, sb);

            case Break bk:
                {
                    var (_, el) = getLoopLabels(bk.TargetLoop);
                    sb.AppendLine($"    br label %{el} ; Break {bk.Location}");
                    sb.AppendLine($"{valueCounter++}:");
                    return "";
                }

            case Continue ct:
                {
                    var (st, _) = getLoopLabels(ct.TargetLoop);
                    sb.AppendLine($"    br label %{st} ; Continue {ct.Location}");
                    sb.AppendLine($"{valueCounter++}:");
                    return "";
                }

            case InifiniteLoop il:
                {
                    var (sl, el) = getLoopLabels(il);
                    sb.AppendLine($"    br label %{sl}");
                    sb.AppendLine($"{sl}:   ; loop {il.Location}");
                    generateExecutable(il.Body, locals, sb);
                    sb.AppendLine($"    br label %{sl}");
                    sb.AppendLine($"{el}:");
                    return "";
                }

            case EnumMemberAccess ema:
                {
                    sb.AppendLine($"    ; Enum member {ema.EnumType.Name}.{ema.MemberName} = {ema.Value}");
                    return ema.Value.ToString();
                }

            case EnumOfMethod eom:
                return generateEnumOf(eom, locals, sb);

            case EnumNameMethod enm:
                return generateEnumName(enm, locals, sb);

            case StructLiteral sl:
                return generateStructLiteral(sl, locals, sb);

            // case StaticFunctionValue sfv:
            //     {
            //         sb.AppendLine($"    ; Static function value {sfv.Function.FullName}");
            //         string result = getNextValueName();
            //         sb.AppendLine($"    {result} = bitcast {getLlvmType(sfv.Function.ReturnType)} ()* @{sfv.Function.FullName} to i64");
            //         return result;
            //     }

            default:
                throw new NotImplementedException($"Code generation for {exe.GetType().Name} not implemented");
        }
    }

    private string generateCodeBlock(CodeBlock cb, INameContainer locals, StringBuilder sb)
    {
        sb.AppendLine($"        ; CodeBlock {cb.Location}");
        foreach (var local in cb.Locals.Variables)
        {
            sb.AppendLine($"    %{local.FullName} = alloca {getLlvmType(local.Type)}");
        }

        foreach (var executable in cb.Code)
        {
            generateExecutable(executable, locals, sb);
        }
        return "";
    }

    private string generateBinaryOp(BinaryOperation bo, INameContainer locals, StringBuilder sb)
    {
        switch (bo.OperationType)
        {
            case BinaryOperationTypes.Assign:
                {
                    string rightVal = generateExecutable(bo.Right, locals, sb);
                    string leftPtr = generateLHS(bo.Left, locals, sb);

                    sb.AppendLine($"        ; Assign {bo.Location}");
                    sb.AppendLine($"    store {getLlvmType(bo.Right.ReturnType)} {rightVal}, ptr {leftPtr}");

                    return rightVal;
                }

            case BinaryOperationTypes.Add:
            case BinaryOperationTypes.Subtract:
            case BinaryOperationTypes.Multiply:
            case BinaryOperationTypes.BitwiseAnd:
            case BinaryOperationTypes.BitwiseOr:
            case BinaryOperationTypes.BitwiseXor:
            case BinaryOperationTypes.LeftShift:
            case BinaryOperationTypes.RightShift:
                {
                    string leftVal = generateExecutable(bo.Left, locals, sb);
                    string rightVal = generateExecutable(bo.Right, locals, sb);
                    string result = getNextValueName();

                    string opName = bo.ReturnType is DefaultType.FloatingPoint
                        ? FloatMathOps.GetValueOrDefault(bo.OperationType, "fadd")
                        : MathOps.GetValueOrDefault(bo.OperationType, "add");

                    sb.AppendLine($"    ; {bo.OperationType} {bo.Location}");
                    sb.AppendLine($"    {result} = {opName} {getLlvmType(bo.ReturnType)} {leftVal}, {rightVal}");

                    return result;
                }

            case BinaryOperationTypes.Divide:
                {
                    string leftVal = generateExecutable(bo.Left, locals, sb);
                    string rightVal = generateExecutable(bo.Right, locals, sb);
                    string result = getNextValueName();

                    sb.AppendLine($"    ; Division {bo.Location}");
                    DefaultType.FloatingPoint? fpType = bo.ReturnType as DefaultType.FloatingPoint;
                    if (fpType != null)
                    {
                        sb.AppendLine($"    {result} = fdiv {getLlvmType(bo.ReturnType)} {leftVal}, {rightVal}");
                    }
                    else if (bo.ReturnType is DefaultType.Integer intDiv)
                    {
                        string op = intDiv.IsSigned ? "sdiv" : "udiv";
                        sb.AppendLine($"    {result} = {op} {getLlvmType(bo.ReturnType)} {leftVal}, {rightVal}");
                    }
                    else
                        throw new Exception($"Unknown division type {bo.ReturnType}");

                    return result;
                }

            case BinaryOperationTypes.Remainder:
                {
                    string leftVal = generateExecutable(bo.Left, locals, sb);
                    string rightVal = generateExecutable(bo.Right, locals, sb);
                    string result = getNextValueName();
                    sb.AppendLine($"    ; Remainder {bo.Location}");
                    DefaultType.Integer intMod = (DefaultType.Integer)bo.ReturnType;
                    string op = intMod.IsSigned ? "srem" : "urem";
                    sb.AppendLine($"    {result} = {op} {getLlvmType(bo.ReturnType)} {leftVal}, {rightVal}");
                    return result;
                }

            case BinaryOperationTypes.Less:
            case BinaryOperationTypes.LessEquals:
            case BinaryOperationTypes.Greater:
            case BinaryOperationTypes.GreaterEquals:
            case BinaryOperationTypes.IsEquals:
            case BinaryOperationTypes.NotEquals:
                {
                    string leftVal = generateExecutable(bo.Left, locals, sb);
                    string rightVal = generateExecutable(bo.Right, locals, sb);
                    string result = getNextValueName();

                    string cmpOp = bo.Left.ReturnType is DefaultType.FloatingPoint ? "fcmp ogt" : "icmp";
                    string op = CompareOps.GetValueOrDefault(bo.OperationType, "eq");

                    sb.AppendLine($"        ; {bo.OperationType} {bo.Location}");
                    sb.AppendLine($"    {result} = {cmpOp} {op} {getLlvmType(bo.Left.ReturnType)} {leftVal}, {rightVal}");

                    return result;
                }

            case BinaryOperationTypes.LogicalAnd:
                {
                    string leftVal = generateExecutable(bo.Left, locals, sb);
                    string rightVal = generateExecutable(bo.Right, locals, sb);
                    string result = getNextValueName();

                    sb.AppendLine($"        ; LogicalAnd {bo.Location}");
                    sb.AppendLine($"    {result} = and i1 {leftVal}, {rightVal}");

                    return result;
                }

            case BinaryOperationTypes.LogicalOr:
                {
                    string leftVal = generateExecutable(bo.Left, locals, sb);
                    string rightVal = generateExecutable(bo.Right, locals, sb);
                    string result = getNextValueName();

                    sb.AppendLine($"        ; LogicalOr {bo.Location}");
                    sb.AppendLine($"    {result} = or i1 {leftVal}, {rightVal}");

                    return result;
                }

            default:
                throw new NotImplementedException($"Binary operation {bo.OperationType} not implemented");
        }
    }

    private int getMemberIndex(StructType st, string memberName)
    {
        for (int i = 0; i < st.Members.Length; i++)
        {
            if (st.Members[i].Name == memberName)
                return i;
        }
        throw new Exception($"Member {memberName} not found in struct {st.Name}");
    }

    private string generateUnaryOp(UnaryOperation uo, INameContainer locals, StringBuilder sb)
    {
        switch (uo.OperationType)
        {
            case UnaryOperationTypes.Cast:
                {
                    return generateCast(uo, locals, sb);
                }

            case UnaryOperationTypes.GetReference:
                {
                    sb.AppendLine($"        ; GetReference {uo.Location}");
                    return generateLHS(uo.Operand, locals, sb);

                    // if (uo.Operand is Identifyer ident && ident.Definition is VariableDefinition variable)
                    //     return $"%{variable.FullName}";
                    //
                    // throw new Exception("Unsupported reference operation");
                }

            case UnaryOperationTypes.Minus:
                {
                    string operandVal = generateExecutable(uo.Operand, locals, sb);
                    string result = getNextValueName();

                    sb.AppendLine($"        ; Minus {uo.Location}");
                    if (uo.Operand.ReturnType is DefaultType.FloatingPoint)
                        sb.AppendLine($"    {result} = fsub {getLlvmType(uo.Operand.ReturnType)} -0.0, {operandVal}");
                    else
                        sb.AppendLine($"    {result} = sub {getLlvmType(uo.Operand.ReturnType)} 0, {operandVal}");

                    return result;
                }

            case UnaryOperationTypes.Dereference:
                {
                    string ptrVal = generateExecutable(uo.Operand, locals, sb);
                    string result = getNextValueName();

                    sb.AppendLine($"        ; Dereference {uo.Location}");
                    if (uo.Operand.ReturnType is Pointer ptr)
                        sb.AppendLine($"    {result} = load {getLlvmType(ptr.PointsTo)}, ptr {ptrVal}");

                    return result;
                }

            case UnaryOperationTypes.Increment:
            case UnaryOperationTypes.Decrement:
                return generateIncrement(uo.Operand, uo.OperationType == UnaryOperationTypes.Decrement, false, uo.Location, locals, sb);

            case UnaryOperationTypes.Inverse:
                {
                    string operandVal = generateExecutable(uo.Operand, locals, sb);
                    string result = getNextValueName();

                    sb.AppendLine($"        ; Inverse {uo.Location}");
                    sb.AppendLine($"    {result} = xor i1 {operandVal}, 1");
                    return result;
                }

            default:
                throw new NotImplementedException($"Unary operation {uo.OperationType} not implemented");
        }
    }

    private string generateIncrement(Executable operand, bool isDecrement, bool isPostfix, Location location, INameContainer locals, StringBuilder sb)
    {
        string currentVal = generateExecutable(operand, locals, sb);
        string result = getNextValueName();
        string newVal = getNextValueName();

        sb.AppendLine($"        ; Increment {location}");
        sb.AppendLine($"    {newVal} = {(isDecrement ? "sub" : "add")} {getLlvmType(operand.ReturnType)} {currentVal}, 1");

        if (operand is Identifyer ident && ident.Definition is VariableDefinition variable)
            sb.AppendLine($"    store {getLlvmType(operand.ReturnType)} {newVal}, {getLlvmType(operand.ReturnType)}* %{variable.FullName}");

        if (isPostfix)
            return currentVal;
        else
            return newVal;
    }

    private string generateCast(UnaryOperation uo, INameContainer locals, StringBuilder sb)
    {
        Executable operand = uo.Operand;
        Typ target = uo.ReturnType;
        Typ source  = operand.ReturnType;

        if (source is SizedArray && target is Pointer)
        {
            string array = generateLHS(operand, locals, sb);
            sb.AppendLine($"        ; Cast {operand.ReturnType} to {target} {uo.Location}");
            return array;
        }

        string operandVal = generateExecutable(operand, locals, sb);
        sb.AppendLine($"        ; Cast {operand.ReturnType} to {target} {uo.Location}");

        if (operand.ReturnType is Pointer && target is Pointer)
            return operandVal; // ptr to ptr

        string result = getNextValueName();

        if (operand.ReturnType is DefaultType.Integer fromInt && target is DefaultType.Integer toInt)
        {
            int fInt = fromInt.Size == 0 ? 8 : fromInt.Size;
            if (fInt < toInt.Size)
            {
                if (fromInt.IsSigned)
                    sb.AppendLine($"    {result} = sext {getLlvmType(fromInt)} {operandVal} to {getLlvmType(toInt)}");
                else
                    sb.AppendLine($"    {result} = zext {getLlvmType(fromInt)} {operandVal} to {getLlvmType(toInt)}");
            }
            else if (fInt > toInt.Size)
                sb.AppendLine($"    {result} = trunc {getLlvmType(fromInt)} {operandVal} to {getLlvmType(toInt)}");
            else
                return operandVal;

            return result;
        }
        else if (operand.ReturnType is DefaultType.FloatingPoint fromFp && target is DefaultType.FloatingPoint toFp)
        {
            if (fromFp.Size < toFp.Size)
                sb.AppendLine($"    {result} = fpext {getLlvmType(fromFp)} {operandVal} to {getLlvmType(toFp)}");
            else if (fromFp.Size > toFp.Size)
                sb.AppendLine($"    {result} = fptrunc {getLlvmType(fromFp)} {operandVal} to {getLlvmType(toFp)}");
            else
                return operandVal;

            return result;
        }
        else if (operand.ReturnType is DefaultType.Integer intType && target is DefaultType.FloatingPoint fpType)
        {
            if (intType.IsSigned)
                sb.AppendLine($"    {result} = sitofp {getLlvmType(intType)} {operandVal} to {getLlvmType(fpType)}");
            else
                sb.AppendLine($"    {result} = uitofp {getLlvmType(intType)} {operandVal} to {getLlvmType(fpType)}");
            return result;
        }
        else if (operand.ReturnType is DefaultType.FloatingPoint fpType2 && target is DefaultType.Integer intType2)
        {
            if (intType2.IsSigned)
                sb.AppendLine($"    {result} = fptosi {getLlvmType(fpType2)} {operandVal} to {getLlvmType(intType2)}");
            else
                sb.AppendLine($"    {result} = fptoui {getLlvmType(fpType2)} {operandVal} to {getLlvmType(intType2)}");
            return result;
        }
        else if (operand.ReturnType is DefaultType.Integer sInt && target is Pointer)
        {
            if (sInt.Size != 0 && (sInt.Size != 8 || sInt.IsSigned))
                throw new Exception($"Uncastable int {sInt}");
            sb.AppendLine($"    {result} = inttoptr i64 {operandVal} to ptr");
            return result;
        }
        else if (source is Pointer && target is SizedArray)
        {
            sb.AppendLine($"    {result} = load {getLlvmType(target)}, ptr {operandVal}");
            return result;
        }
        else
            throw new Exception($"Unknown cast {operand.ReturnType} to {target}");
    }

    private string generateFunctionCall(FunctionCall fc, INameContainer locals, StringBuilder sb)
    {
        sb.AppendLine($"        ; Function call args {fc.Location}");
        var args = new List<(string value, string type)>();

        foreach (var arg in fc.Args)
        {
            string argVal = generateExecutable(arg, locals, sb);
            args.Add((argVal, getLlvmType(arg.ReturnType)));
        }

        string argList = string.Join(", ", args.Select(a => $"{a.type} {a.value}"));
        string retType = getLlvmType(fc.ReturnType);

        // Get function pointer - FunctionPointer may be an Identifier with a FunctionDefinition
        sb.AppendLine($"        ; Func call");
        string funcName = generateExecutable(fc.FunctionPointer, locals, sb);
        // if (fc.FunctionPointer is Identifyer ident && ident.Definition is FunctionDefinition fnDef)
        //     funcName = fnDef.FullName;

        if (fc.ReturnType == DefaultType.Void)
        {
            sb.AppendLine($"    call {retType} {funcName}({argList})");
            return "";
        }
        else
        {
            string result = getNextValueName();
            sb.AppendLine($"    {result} = call {retType} {funcName}({argList})");
            return result;
        }
    }

    private string generateMethodCall(MethodCall mc, INameContainer locals, StringBuilder sb)
    {
        sb.AppendLine($"        ; Method call {mc.Location}");
        throw new NotImplementedException("Method calls");
    }

    private string generateMethodValue(MethodValue mv, INameContainer locals, StringBuilder sb)
    {
        sb.AppendLine($"        ; Method value {mv.Location}");
        throw new NotImplementedException("Method value");
    }

    private string generateIdentifier(Identifyer id, INameContainer locals, StringBuilder sb)
    {
        sb.AppendLine($"        ; Identifier {id.Definition?.GetType().Name} {id.Location}");

        if (id.Definition is VariableDefinition varDef)
        {
            // string allocName = localVariables[varDef.Name];
            string result = getNextValueName();
            sb.AppendLine($"    {result} = load {getLlvmType(varDef.Type)}, ptr %{varDef.FullName}");
            return result;
        }
        else if (id.Definition is FunctionDefinition fnDef)
        {
            if (fnDef.Modifyers.Contains(Keywords.External))
                return $"@{fnDef.Name}";
            else
                return $"@{fnDef.FullName}";
        }
        else
            throw new Exception($"Unsupported def {id.Definition}");
    }

    private string generateGetMember(GetMember gm, INameContainer locals, StringBuilder sb)
    {
        string baseVal = generateExecutable(gm.Operand, locals, sb);
        sb.AppendLine($"        ; GetMember {gm.Location}");

        if (gm.Operand.ReturnType is StructType st)
        {
            int memberIdx = getMemberIndex(st, gm.Member.Value);
            // var memberType = st.GetStructMember(gm.Member.Value);

            string result = getNextValueName();
            // sb.AppendLine($"    {elemPtr} = getelementptr {getLlvmType(st)}, ptr {basePtr}, i32 0, i32 {memberIdx}");
            sb.AppendLine($"    {result} = extractvalue {getLlvmType(gm.Operand.ReturnType)} {baseVal}, {memberIdx}");

            return result;
        }
        else
            throw new Exception("Get member for non struct type");
    }

    private string generateGetElement(GetElement ge, INameContainer locals, StringBuilder sb)
    {
        string array = generateExecutable(ge.Operand, locals, sb);
        string index = generateExecutable(ge.Index, locals, sb);
        string tmp = getNextValueName();
        string elPtr = getNextValueName();
        string result = getNextValueName();
        sb.AppendLine($"        ; GetElement {ge.Location}");
        sb.AppendLine($"    {tmp} = alloca {getLlvmType(ge.Operand.ReturnType)}");
        sb.AppendLine($"    store {getLlvmType(ge.Operand.ReturnType)} {array}, ptr {tmp}");
        sb.AppendLine($"    {elPtr} = getelementptr {getLlvmType(ge.Operand.ReturnType)}, ptr {tmp}, i32 0, {getLlvmType(ge.Index.ReturnType)} {index}");
        sb.AppendLine($"    {result} = load {getLlvmType(ge.ReturnType)}, ptr {elPtr}");
        return result;
    }

    // private string generateGetElementAddress(GetElement ge, INameContainer locals, StringBuilder sb)
    // {
    //     string indexVal = generateExecutable(ge.Index, locals, sb);
    //     string basePtr = generateExecutable(ge.Operand, locals, sb);
    //
    //     Typ elementType;
    //     if (ge.Operand.ReturnType is SizedArray sa)
    //         elementType = sa.ElementType;
    //     else if (ge.Operand.ReturnType is Pointer p)
    //         elementType = p.PointsTo;
    //     else
    //         throw new Exception("Invalid array/pointer access");
    //
    //     string elemPtr = getNextValueName();
    //     sb.AppendLine($"        ; Get element address {ge.Operand} {ge.Location}");
    //     sb.AppendLine($"    {elemPtr} = getelementptr {getLlvmType(elementType)}, ptr {basePtr}, i64 {indexVal}");
    //     return elemPtr;
    // }

    private string generateStructLiteral(StructLiteral sl, INameContainer locals, StringBuilder sb)
    {
        sb.AppendLine($"    ; Struct literal {sl.StructType.Name}");

        string result = getNextValueName();
        sb.AppendLine($"    {result} = alloca {getLlvmType(sl.StructType)}");

        for (int i = 0; i < sl.FieldValues.Length; i++)
        {
            string fieldVal = generateExecutable(sl.FieldValues[i], locals, sb);
            var fieldType = sl.StructType.Members[i].Type;
            string fieldPtr = getNextValueName();
            sb.AppendLine($"    {fieldPtr} = getelementptr {getLlvmType(sl.StructType)}, {getLlvmType(sl.StructType)}* {result}, i32 0, i32 {i}");
            sb.AppendLine($"    store {getLlvmType(fieldType)} {fieldVal}, {getLlvmType(fieldType)}* {fieldPtr}");
        }

        return result;
    }

    private string generateEnumOf(EnumOfMethod eom, INameContainer locals, StringBuilder sb)
    {
        string namePtr = generateExecutable(eom.NameArgument, locals, sb);
        sb.AppendLine($"    ; Enum.of {eom.EnumType.Name}");

        int endLabel = ifsCounter++;
        string result = getNextValueName();
        sb.AppendLine($"    {result} = alloca i64");

        foreach (var member in eom.EnumType.Members)
        {
            string memberNamePtr = getNextValueName();
            int strIdx = stringLiterals.Count;
            stringLiterals.Add(member.Name);
            sb.AppendLine($"    {memberNamePtr} = getelementptr [{member.Name.Length + 1} x i8], [{member.Name.Length + 1} x i8]* @str{strIdx}, i64 0, i64 0");

            string cmpResult = getNextValueName();
            sb.AppendLine($"    {cmpResult} = call i32 @strcmp(i8* {namePtr}, i8* {memberNamePtr})");
            sb.AppendLine($"    br i1 %cond, label %enum_of_match_{endLabel}, label %enum_of_next_{endLabel}");
            sb.AppendLine($"enum_of_match_{endLabel}:");
            sb.AppendLine($"    store i64 {member.Value}, i64* {result}");
            sb.AppendLine($"    br label %enum_of_end_{endLabel}");
            sb.AppendLine($"enum_of_next_{endLabel}:");
        }

        sb.AppendLine($"enum_of_end_{endLabel}:");
        string loadResult = getNextValueName();
        sb.AppendLine($"    {loadResult} = load i64, i64* {result}");
        return loadResult;
    }

    private string generateEnumName(EnumNameMethod enm, INameContainer locals, StringBuilder sb)
    {
        string enumVal = generateExecutable(enm.EnumValue, locals, sb);
        sb.AppendLine($"    ; Enum.name {enm.EnumType.Name}");

        int endLabel = ifsCounter++;
        string result = getNextValueName();
        sb.AppendLine($"    {result} = alloca i8*");

        foreach (var member in enm.EnumType.Members)
        {
            int strIdx = stringLiterals.Count;
            stringLiterals.Add(member.Name);
            string cmpResult = getNextValueName();
            sb.AppendLine($"    {cmpResult} = icmp eq i64 {enumVal}, {member.Value}");
            sb.AppendLine($"    br i1 {cmpResult}, label %enum_name_match_{endLabel}, label %enum_name_next_{endLabel}");
            sb.AppendLine($"enum_name_match_{endLabel}:");
            sb.AppendLine($"    store i8* getelementptr inbounds ([{member.Name.Length + 1} x i8], [{member.Name.Length + 1} x i8]* @str{strIdx}, i32 0, i32 0), i8** {result}");
            sb.AppendLine($"    br label %enum_name_end_{endLabel}");
            sb.AppendLine($"enum_name_next_{endLabel}:");
        }

        sb.AppendLine($"enum_name_end_{endLabel}:");
        string loadResult = getNextValueName();
        sb.AppendLine($"    {loadResult} = load i8*, i8** {result}");
        return loadResult;
    }

    private string generateLHS(Executable executable, INameContainer locals, StringBuilder sb)
    {
        string returnVal;
        switch (executable)
        {
            case Identifyer ident:
                sb.AppendLine($"        ; lhs Identifyer {ident.Location}");
                if (ident.Definition is VariableDefinition)
                    returnVal = $"%{ident.Definition.FullName}";
                else if (ident.Definition is FunctionDefinition)
                {
                    if (ident.Definition.Modifyers.Contains(Keywords.External))
                        returnVal = $"@{ident.Definition.Name}";
                    else
                        returnVal = $"@{ident.Definition.FullName}";
                }
                else
                    throw new Exception($"Can not get ptr for {ident.Definition}");

                break;

            case GetMember gm:
                {
                    string basePtr = generateLHS(gm.Operand, locals, sb);
                    sb.AppendLine($"        ; lhs GetMember {gm.Location}");

                    if (gm.Operand.ReturnType is StructType st)
                    {
                        int memberIdx = getMemberIndex(st, gm.Member.Value);

                        string elemPtr = getNextValueName();
                        sb.AppendLine($"    {elemPtr} = getelementptr {getLlvmType(st)}, ptr {
                                basePtr}, i32 0, i32 {memberIdx}");

                        returnVal = elemPtr;
                    }
                    else
                        throw new Exception("Get member for non struct type");

                    break;
                }

            case GetElement ge:
                {
                    string array = generateLHS(ge.Operand, locals, sb);
                    string index = generateExecutable(ge.Index, locals, sb);
                    string elPtr = getNextValueName();
                    sb.AppendLine($"        ; GetElement {ge.Location}");
                    sb.AppendLine($"    {elPtr} = getelementptr {getLlvmType(ge.Operand.ReturnType)}, ptr {array}, i32 0, {getLlvmType(ge.Index.ReturnType)} {index}");
                    returnVal = elPtr;
                    break;
                }

            default:
                throw new Exception($"Can not generate ptr for {executable}");
        }

        return returnVal;
    }
}
