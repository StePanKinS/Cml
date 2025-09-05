// using System.Text;
// using Cml.Parsing;

// namespace Cml;

// static class CodeGenFasm
// {
//     public static string Generate(ParsedFile parsed)
//     {
//         StringBuilder sb = new();
//         sb.Append("format elf64\n");
//         sb.Append("section \".text\" executale\n");
//         sb.Append("public main\n");

//         foreach (Definition def in parsed.Definitions.Names.Values)
//         {
//             if (def is FunctionDefinition funcDef)
//                 generateFunction(sb, funcDef);
//         }

//         return sb.ToString();
//     }

//     private static void generateFunction(StringBuilder sb, FunctionDefinition funcDef)
//     {
//         if (funcDef.IsExtern)
//         {
//             sb.Append($"extrn {funcDef.Name}");
//             return;
//         }

//         sb.Append($"{funcDef.Name}:\n");
//         sb.Append($"\tpush rbp");
//         sb.Append($"\tmov rbp, rsp");
//         foreach (var structdef in (StructDefinition[])funcDef.LocalNameContext!.Names.Values.ToArray()) {
            
//         }
//     }
// }