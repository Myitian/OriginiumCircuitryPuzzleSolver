using System.Diagnostics;
using System.Drawing;

namespace OriginiumCircuitryPuzzleSolver;

public record struct Grid
{
    private readonly Memory<byte> _data;

    public Grid(int width, int height)
    {
        Width = width;
        Height = height;
        _data = new byte[checked(width * height)];
    }
    public Grid(int width, Memory<byte> data)
    {
        Width = width;
        Height = data.Length / width;
        _data = data[..(width * Height)];
    }

    public int Width { readonly get; private set; }
    public int Height { readonly get; private set; }
    public readonly Span<byte> Data => _data.Span;

    public readonly Grid DeepClone()
    {
        Grid result = new(Width, Height);
        Data.CopyTo(result.Data);
        return result;
    }

    public void ClockwiseRotate()
    {
        Span<byte> data = Data;
        Span<byte> buffer = stackalloc byte[data.Length];
        int width = Height;
        int height = Width;
        for (int y = 0; y < Height; y++)
        {
            int offset = y * Width;
            for (int x = 0; x < Width; x++)
            {
                int oldIndex = offset + x;
                int newIndex = x * width + (Height - 1 - y);
                buffer[newIndex] = data[oldIndex];
            }
        }
        Height = height;
        Width = width;
        buffer.CopyTo(data);
    }

    public readonly Rectangle GetBoundingBox()
    {
        if (Width == 0 || Height == 0)
            return Rectangle.Empty;
        Span<byte> data = Data;
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = 0;
        int maxY = 0;
        int counter = 0;
        for (int y = 0; y < Height; y++)
        {
            int offset = y * Width;
            for (int x = 0; x < Width; x++)
            {
                if (data[offset + x] != 0)
                {
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                    counter++;
                }
            }
        }
        if (counter == 0)
            return Rectangle.Empty;
        return new(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    public readonly Grid Trim(bool alwaysCopy = false)
    {
        Rectangle bbox = GetBoundingBox();
        if (bbox.Width == Width)
        {
            Memory<byte> slice = _data.Slice(bbox.Y * Width, bbox.Height * Width);
            if (!alwaysCopy)
                return new(Width, slice);
            Grid result = new(bbox.Width, bbox.Height);
            slice.CopyTo(result._data);
            return result;
        }
        else
        {
            Grid result = new(bbox.Width, bbox.Height);
            for (int y = 0; y < bbox.Height; y++)
            {
                Memory<byte> srcSlice = _data.Slice((bbox.Y + y) * Width, bbox.Width);
                srcSlice.CopyTo(result._data[(y * bbox.Width)..]);
            }
            return result;
        }
    }

    public readonly bool AddTo(Grid dst, int offsetX, int offsetY)
    {
        Span<byte> srcData = Data;
        Span<byte> dstData = dst.Data;
        for (int srcY = 0, dstY = offsetY; srcY < Height; srcY++, dstY++)
        {
            int srcOffset = srcY * Width;
            int dstOffset = dstY * dst.Width;
            for (int srcX = 0, dstX = offsetX; srcX < Width; srcX++, dstX++)
            {
                byte value = srcData[srcOffset + srcX];
                if (value == 0)
                    continue;
                if (dstX >= dst.Width || dstY >= dst.Height)
                    return false;
                if ((dstData[dstOffset + dstX] += value) > (int)GridSlot.Occupied)
                    return false;
            }
        }
        return true;
    }

    public override readonly string ToString()
    {
        Span<char> buffer = stackalloc char[Data.Length + Height];
        return TryFormat(buffer) ? new(buffer) : throw new UnreachableException();
    }
    public readonly bool TryFormat(Span<char> chars)
    {
        if (chars.Length < Data.Length + Height)
            return false;
        for (int y = 0; y < Height; y++)
        {
            int offset = y * Width;
            for (int x = 0; x < Width; x++)
            {
                int i = offset + x;
                chars[i + y] = Data[i] switch
                {
                    0 => '.',
                    1 => '*',
                    2 => '0',
                    _ => '?'
                };
            }
            chars[offset + y + Width] = '\n';
        }
        return true;
    }
    public readonly void TryFormatOccupiedOnly(Span<char> chars, char ch, int offsetX, int offsetY, int dstWidth, int dstHeight)
    {
        for (int srcY = 0; srcY < Height; srcY++)
        {
            int dstY = srcY + offsetY;
            if (dstY >= dstHeight)
                break;
            if (dstY < 0)
                continue;
            int srcOffset = srcY * Width;
            int dstOffset = (srcY + offsetY) * dstWidth;
            for (int srcX = 0; srcX < Width; srcX++)
            {
                int dstX = srcX + offsetX;
                if (dstX >= dstWidth)
                    break;
                if (dstX < 0)
                    continue;
                int srcIndex = srcOffset + srcX;
                int dstIndex = dstOffset + dstX;
                if (Data[srcIndex] is (byte)GridSlot.Occupied)
                    chars[dstIndex + dstY] = ch;
            }
        }
    }
    public static Grid FromString(ReadOnlySpan<char> chars)
    {
        int width = 0;
        int height = 0;
        int x = 0;
        int offset = 0;
        foreach (char c in chars)
        {
            switch (c)
            {
                case '.':
                case '*':
                case '0':
                    x++;
                    break;
                case '\n':
                    width = Math.Max(width, x);
                    x = 0;
                    height++;
                    break;
            }
        }
        if (x != 0)
        {
            width = Math.Max(width, x);
            height++;
        }
        Grid result = new(width, height);
        foreach (char c in chars)
        {
            switch (c)
            {
                case '.':
                    result.Data[offset + x++] = 0;
                    break;
                case '*':
                    result.Data[offset + x++] = 1;
                    break;
                case '0':
                    result.Data[offset + x++] = 2;
                    break;
                case '\n':
                    x = 0;
                    offset += width;
                    break;
            }
        }
        return result;
    }
}