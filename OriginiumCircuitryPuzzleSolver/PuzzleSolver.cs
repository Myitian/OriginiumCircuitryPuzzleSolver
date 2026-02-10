using System.Drawing;

namespace OriginiumCircuitryPuzzleSolver;

public static class PuzzleSolver
{
    public static bool ValidateComponent(Grid component)
    {
        return !component.Data[..(component.Width * component.Height)]
            .ContainsAnyExcept([(byte)GridSlot.Empty, (byte)GridSlot.Occupied]);
    }
    public static bool ValidateRequirements(
        Grid grid,
        ReadOnlySpan<int> colRequirements,
        ReadOnlySpan<int> rowRequirements)
    {
        return colRequirements.Length == grid.Width
            && rowRequirements.Length == grid.Height
            && !colRequirements.ContainsAnyExceptInRange(0, grid.Height)
            && !rowRequirements.ContainsAnyExceptInRange(0, grid.Width);
    }
    public static bool Solve(
        Grid grid,
        ReadOnlySpan<Grid> components,
        ReadOnlySpan<int> colRequirements,
        ReadOnlySpan<int> rowRequirements,
        Span<PositionWithRotation> result)
    {
        Context[] stack = new Context[components.Length];
        for (int i = 0; i < components.Length; i++)
            stack[i] = new(grid, components[i]);
        bool succeed = Place(grid, components.Length, stack, colRequirements, rowRequirements);
        if (succeed)
        {
            for (int i = 0, len = Math.Min(components.Length, result.Length); i < len; i++)
                result[i] = stack[i].PositionWithRotation;
        }
        return succeed;
    }
    static bool Place(
        Grid baseGrid,
        int depth,
        Span<Context> stack,
        ReadOnlySpan<int> colRequirements,
        ReadOnlySpan<int> rowRequirements)
    {
        if (depth == 0)
        {
            ref Context ctx = ref stack[0];
            Span<int> colSum = stackalloc int[colRequirements.Length];
            Span<int> rowSum = stackalloc int[rowRequirements.Length];
            ReadOnlySpan<byte> data = ctx.WorkingGrid.Data;
            for (int y = 0; y < ctx.WorkingGrid.Height; y++)
            {
                int offset = y * ctx.WorkingGrid.Width;
                for (int x = 0; x < ctx.WorkingGrid.Width; x++)
                {
                    if (data[offset + x] == 2)
                    {
                        colSum[x]++;
                        rowSum[y]++;
                    }
                }
            }
            return colSum.SequenceEqual(colRequirements) && rowSum.SequenceEqual(rowRequirements);
        }
        else
        {
            //Console.WriteLine($"Depth: {depth}");
            ref Context ctx = ref stack[--depth];
            if (ctx.ComponentBoundingBox.Width == 0 || ctx.ComponentBoundingBox.Height == 0)
                return Place(baseGrid, depth, stack, colRequirements, rowRequirements);
            for (int i = 0; i < 4; i++)
            {
                int offsetXStart = ctx.ComponentBoundingBox.Left;
                int offsetXEnd = ctx.WorkingGrid.Width - ctx.ComponentBoundingBox.Right;
                int offsetYStart = ctx.ComponentBoundingBox.Top;
                int offsetYEnd = ctx.WorkingGrid.Height - ctx.ComponentBoundingBox.Bottom;
                for (int offsetY = offsetYStart; offsetY <= offsetYEnd; offsetY++)
                {
                    for (int offsetX = offsetXStart; offsetX <= offsetXEnd; offsetX++)
                    {
                        baseGrid.Data.CopyTo(ctx.WorkingGrid.Data);
                        if (!ctx.Component.AddTo(ctx.WorkingGrid, offsetX, offsetY))
                        {
                            //Console.WriteLine($"Failed to add at ({offsetX}, {offsetY})");
                            continue;
                        }
                        //Console.WriteLine($"""
                        //    at ({offsetX}, {offsetY}) with rotation {ctx.PositionWithRotation.Rotation}:
                        //    {ctx.WorkingGrid}
                        //    """);
                        if (Place(ctx.WorkingGrid, depth, stack, colRequirements, rowRequirements))
                        {
                            ctx.PositionWithRotation.X = offsetX - offsetXStart;
                            ctx.PositionWithRotation.Y = offsetY - offsetYStart;
                            return true;
                        }
                    }
                }
                ctx.ClockwiseRotate();
            }
            return false;
        }
    }
    struct Context(Grid grid, Grid component)
    {
        public readonly Grid WorkingGrid = new(grid.Width, grid.Height);
        public Grid Component = component.DeepClone();
        public Rectangle ComponentBoundingBox = component.GetBoundingBox();
        public PositionWithRotation PositionWithRotation;

        public void ClockwiseRotate()
        {
            Component.ClockwiseRotate();
            (ComponentBoundingBox.X, ComponentBoundingBox.Y) = (ComponentBoundingBox.Y, ComponentBoundingBox.X);
            (ComponentBoundingBox.Width, ComponentBoundingBox.Height) = (ComponentBoundingBox.Height, ComponentBoundingBox.Width);
            PositionWithRotation.Rotation = (PositionWithRotation.Rotation + 1) % 4;
        }
    }
}