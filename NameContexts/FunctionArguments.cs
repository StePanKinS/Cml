using System.Diagnostics.CodeAnalysis;

namespace Cml.NameContexts;

public class FunctionArguments(INameContainer parent, FunctionDefinition func) : INameContainer
{
    public INameContainer Parent = parent;
    public List<VariableDefinition> Arguments = [];
    public FunctionDefinition ParentFunction = func;

    private int size = -1;
    public int Size
    {
        get
        {
            if (size == -1)
            {
                var (intCount, floatCount, _) = GetClassCount();
                size = (intCount + floatCount) * 8;
            }
            return size;
        }
    }

    public bool Append(Definition definition)
    {
        if (definition is not VariableDefinition variable)
            throw new ArgumentException($"Only {nameof(VariableDefinition)} can be added to {nameof(FunctionArguments)}");

        if ((from def in (IEnumerable<Definition>)Arguments
             where def.Name == definition.Name
             select def).ToArray().Length > 0)
            return false;

        Arguments.Add(variable);
        return true;
    }

    public bool Append(Token[] type, Token<string> name)
        => Append(new VariableDefinition(name.Value, type, 
            ParentFunction, [], new Location(type.TokensLocation(), name.Location)));

    public int GetVariableOffset(VariableDefinition variable)
    {
        if (!Arguments.Contains(variable))
            return Parent.GetVariableOffset(variable);

        int intCnt = 0;
        int fltCnt = 0;
        int memCnt = 0;
        bool inMemory = false;

        foreach (var v in Arguments)
        {
            if (v.Type is DefaultType.Integer || v.Type is Pointer)
            {
                if (intCnt < 6)
                    intCnt++;
                else
                {
                    memCnt++;
                    if (variable.Type is DefaultType.Integer || variable.Type is Pointer)
                        inMemory = true;
                }
            }
            else if (v.Type is DefaultType.FloatingPoint)
            {
                if (fltCnt < 8)
                    intCnt++;
                else
                {
                    memCnt++;
                    if (variable.Type is DefaultType.FloatingPoint)
                        inMemory = true;
                }
            }
            else
                throw new NotImplementedException("Only integer and pointer argument types are supported");


            if (v == variable)
            {
                if (inMemory)
                    return 16 + (memCnt - 1) * 8;
                else
                    return - (intCnt + fltCnt) * 8;
            }
            
        }

        return 1;
    }

    public bool TryGetName(string name, [MaybeNullWhen(false)] out Definition definition)
    {
        var defs = (from def in (IEnumerable<Definition>)Arguments
                    where def.Name == name
                    select def).ToArray();
        if (defs.Length == 1)
        {
            definition = defs[0];
            return true;
        }

        if (defs.Length > 1)
            throw new Exception($"Found several `{name}` names");

        return Parent.TryGetName(name, out definition);
    }

    public (int intCount, int floatCount, int stackCount) GetClassCount()
        => GetClassCount(Arguments.Select(i => i.Type));

    public static (int intCount, int floatCount, int stackCount) GetClassCount(IEnumerable<Typ> types)
    {
        int intCount = 0;
        int floatCount = 0;
        int stackCount = 0;

        foreach (var i in types)
        {
            if (i is DefaultType.Integer
                || i is Pointer)
            {
                if (intCount < 6)
                    intCount++;
                else
                    stackCount++;
            }
            else if (i is DefaultType.FloatingPoint)
            {
                if (floatCount < 8)
                    floatCount++;
                else
                    stackCount++;
            }
            else
                throw new Exception($"Unsupported type {i} in generateFunctionCall");
        }

        return (intCount, floatCount, stackCount);
    }

    public bool TryGetType(string name, [MaybeNullWhen(false)] out Typ type)
        => Parent.TryGetType(name, out type);
}