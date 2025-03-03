namespace Cml.Parsing;

internal class StructureInitializer(Dictionary<string, Executable> values, Location location) : Executable(location)
{
    public Dictionary<string, Executable> Values = values;

    public new const int Priority = 0;
    public new const bool IsRightToLeft = false;
}
