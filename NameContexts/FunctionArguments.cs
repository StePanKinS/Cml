using System.Diagnostics.CodeAnalysis;

namespace Cml.NameContexts;

public class FunctionArguments(INameContainer parent, FunctionDefinition func) : INameContainer
{
    public INameContainer Parent = parent;
    public List<VariableDefinition> Variables = [];
    public FunctionDefinition ParentFunction = func;

    private int size = -1;
    public int Size
    {
        get
        {
            if (size == -1)
                size = GetIntCount() * 8;
            return size;
        }
    }

    public bool Append(Definition definition)
    {
        if (definition is not VariableDefinition variable)
            throw new ArgumentException($"Only {nameof(VariableDefinition)} can be added to {nameof(FunctionArguments)}");

        if ((from def in (IEnumerable<Definition>)Variables
             where def.Name == definition.Name
             select def).ToArray().Length > 0)
            return false;

        Variables.Add(variable);
        return true;
    }

    public bool Append(Token<string> type, Token<string> name)
        => Append(new VariableDefinition(name.Value, type.Value, ParentFunction, [], new Location(type, name)));

    public int GetVariableOffset(VariableDefinition variable)
    {
        if (!Variables.Contains(variable))
            return Parent.GetVariableOffset(variable);

        // int offset = 16; // Skip return address and old base pointer
        // foreach (var v in Variables)
        // {
        //     if (v == variable)
        //         return offset;
        //     offset += (v.ValueType.Size + 7) & ~7;
        // }

        int intCnt = 0;
        int memCnt = 0;
        bool memInt = false;

        foreach (var v in Variables)
        {
            if (v.ValueType is DefaultType.Integer || v.ValueType is Pointer)
                if (intCnt == 6)
                {
                    memCnt++;
                    memInt = true;
                }
                else
                    intCnt++;
            else
                throw new NotImplementedException("Only integer and pointer argument types are supported");


            if (v == variable)
            {
                if (memInt)
                    return 16 + (memCnt - 1) * 8;
                else
                    return - intCnt * 8;
            }
            
        }

        return 1;
    }

    public bool TryGet(string name, [MaybeNullWhen(false)] out Definition definition)
    {
        var defs = (from def in (IEnumerable<Definition>)Variables
                    where def.Name == name
                    select def).ToArray();
        if (defs.Length == 1)
        {
            definition = defs[0];
            return true;
        }

        if (defs.Length > 1)
            throw new Exception($"Found several `{name}` names");

        return Parent.TryGet(name, out definition);
    }

    public int GetIntCount()
    {
        int count = 0;
        foreach (var v in Variables.Select(v => v.ValueType))
        {
            if (v is DefaultType.Integer || v is Pointer)
                count++;
        }
        return count;
    }

    public bool TryGetType(string name, [MaybeNullWhen(false)] out StructDefinition type)
        => Parent.TryGetType(name, out type);
}