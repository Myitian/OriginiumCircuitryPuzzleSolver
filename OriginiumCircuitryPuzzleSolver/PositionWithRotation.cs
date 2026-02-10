using System.Drawing;

namespace OriginiumCircuitryPuzzleSolver;

public record struct PositionWithRotation : IEquatable<PositionWithRotation>
{
    public int X { readonly get; set; }
    public int Y { readonly get; set; }
    public int Rotation { readonly get; set; }
    public Point Position
    {
        readonly get => new(X, Y);
        set
        {
            X = value.X;
            Y = value.Y;
        }
    }

    public PositionWithRotation(Point point, int rotation) : this(point.X, point.Y, rotation) { }
    public PositionWithRotation(int x, int y, int rotation)
    {
        X = x;
        Y = y;
        Rotation = rotation;
    }
}