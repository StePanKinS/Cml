using System.Diagnostics.CodeAnalysis;
using Cml.Parsing.Definitions;

namespace Cml.Parsing;

public class NameContext(NameContext? parent)
{
    public NameContext? Parent = parent;
    public List<NamespaceDefinition> Namespaces = [];
    public List<StructDefinition> Structs = [];
    public List<FunctionDefinition> Functions = [];
    public List<VariableDefinition> Variables = [];

    
    public bool Append(Definition definition)
    {
        if ((from def in (IEnumerable<Definition>)[.. Namespaces, .. Structs, .. Functions, .. Variables]
             where def.Name == definition.Name
             select def).ToArray().Length > 0)
            return false;

        if (definition is NamespaceDefinition nmsp)
        {
            Namespaces.Add(nmsp);
            return true;
        }
        if (definition is StructDefinition struc)
        {
            Structs.Add(struc);
            return true;
        }
        if (definition is FunctionDefinition func)
        {
            Functions.Add(func);
            return true;
        }
        if (definition is VariableDefinition variable)
        {
            Variables.Add(variable);
            return true;
        }

        throw new ArgumentException($"Unexpected type {definition.GetType().FullName} in {nameof(definition)}");
    }

    public bool TryGetName(string name, [MaybeNullWhen(false)] out Definition definition)
    {
        var defs = (from def in (IEnumerable<Definition>)[.. Namespaces, .. Structs, .. Functions, .. Variables]
                    where def.Name == name
                    select def).ToArray();
        if (defs.Length == 1)
        {
            definition = defs[0];
            return true;
        }

        if (defs.Length > 1)
            throw new Exception($"Found several `{name}` names");

        if (Parent != null)
            return Parent.TryGetName(name, out definition);

        definition = null;
        return false;
    }
}