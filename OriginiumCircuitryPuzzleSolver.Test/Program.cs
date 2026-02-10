using OriginiumCircuitryPuzzleSolver;
using System.Runtime.InteropServices;
using System.Text;

Console.WriteLine("""
    Grid:
      1 2 3 4 - column requirements
    1 . . . .
    2 . . . .
    3 . . . .
    4 . . . .
     \
      row requirements

    . - Empty
    * - Unavailable
    0 - Occupied
    """);
ReadGrid(
    out Grid grid,
    out ReadOnlySpan<int> colRequirements,
    out ReadOnlySpan<int> rowRequirements);
StringBuilder sb = new();
List<Grid> components = [];
while (true)
{
    bool? read = ReadComponent(out Grid component, sb);
    switch (read)
    {
        case true:
            components.Add(component);
            continue;
        case false:
            Console.WriteLine("Invalid component!");
            continue;
    }
    break;
}
ReadOnlySpan<Grid> componentSpan = CollectionsMarshal.AsSpan(components);
Span<PositionWithRotation> result = stackalloc PositionWithRotation[componentSpan.Length];
if (!PuzzleSolver.Solve(grid, componentSpan, colRequirements, rowRequirements, result))
{
    Console.WriteLine("Failed!");
    return;
}
Console.WriteLine("Solution:");
Span<char> buffer = stackalloc char[grid.Data.Length + grid.Height];
grid.TryFormat(buffer);
for (int i = 0; i < componentSpan.Length; i++)
{
    PositionWithRotation pos = result[i];
    Grid component = componentSpan[i].DeepClone();
    while (pos.Rotation > 0)
    {
        pos.Rotation--;
        component.ClockwiseRotate();
    }
    component.TryFormatOccupiedOnly(buffer, (char)('A' + i), pos.X, pos.Y, grid.Width, grid.Height);
}
Console.WriteLine(buffer);


static bool? ReadComponent(out Grid component, StringBuilder? sb = null)
{
    Console.WriteLine("Add a component:");
    if (sb is null)
        sb = new();
    else
        sb.Clear();
    while (Console.ReadLine() is string line and not "")
        sb.AppendLine(line);
    component = Grid.FromString(sb.ToString());
    if (component.Height == 0)
        return null;
    return PuzzleSolver.ValidateComponent(component);
}
static void ReadGrid(
    out Grid grid,
    out ReadOnlySpan<int> colRequirements,
    out ReadOnlySpan<int> rowRequirements,
    StringBuilder? sb = null)
{
    Console.WriteLine("Set the grid:");
    if (sb is null)
        sb = new();
    else
        sb.Clear();
    while (Console.ReadLine() is string line and not "")
        sb.AppendLine(line);
    grid = Grid.FromString(sb.ToString());
    Console.WriteLine("Set the column requirements:");
    ReadSeq(out colRequirements);
    Console.WriteLine("Set the row requirements:");
    ReadSeq(out rowRequirements);
}
static bool ReadSeq(out ReadOnlySpan<int> seq)
{
    seq = [];
    ReadOnlySpan<char> line = Console.ReadLine();
    if (line.IsEmpty)
        return false;
    List<int> values = [];
    foreach (Range range in line.Split(' '))
    {
        ReadOnlySpan<char> slice = line[range].Trim();
        if (slice.IsEmpty)
            continue;
        if (!int.TryParse(slice, out int value))
            return false;
        values.Add(value);
    }
    seq = CollectionsMarshal.AsSpan(values);
    return true;
}