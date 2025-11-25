namespace Cml.Types;

public class Pointer(Typ pointsTo) : Typ(pointsTo.Name + '*', 8)
{
    public Typ PointsTo = pointsTo;

    public override string ToString()
        => $"Pointer({PointsTo.Name})";

    public override bool Equals(object? obj)
        => obj is Pointer p && p.PointsTo == PointsTo;

    public override int GetHashCode()
        => base.GetHashCode();
}
