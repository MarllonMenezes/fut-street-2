namespace FifaStreetPc;

public enum EaglCandidateProjectionPlane
{
    XY,
    XZ,
    YZ,
}

public static class EaglCandidateMeshRenderer
{
    public static int DrawSlot1AttributeProfile(
        SoftwareFrameBuffer frame,
        EaglCandidateMesh mesh,
        int centerX,
        int centerY,
        int pixelSize)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(mesh);
        if (pixelSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelSize));
        }

        var vertices = mesh.Groups.SelectMany(group => group.Vertices).ToArray();
        if (vertices.Length == 0)
        {
            return 0;
        }

        var minX = vertices.Min(vertex => vertex.X);
        var maxX = vertices.Max(vertex => vertex.X);
        var minY = vertices.Min(vertex => vertex.Y);
        var maxY = vertices.Max(vertex => vertex.Y);
        var spanX = Math.Max(0.0001f, maxX - minX);
        var spanY = Math.Max(0.0001f, maxY - minY);
        var scale = pixelSize / Math.Max(spanX, spanY);
        var centerMeshX = (minX + maxX) / 2f;
        var centerMeshY = (minY + maxY) / 2f;
        var drawnGroups = 0;

        foreach (var group in mesh.Groups)
        {
            if (group.Slot1Attributes.Count != group.Vertices.Count)
            {
                continue;
            }

            drawnGroups++;
            foreach (var triangle in group.Triangles)
            {
                if ((uint)triangle.A >= (uint)group.Vertices.Count ||
                    (uint)triangle.B >= (uint)group.Vertices.Count ||
                    (uint)triangle.C >= (uint)group.Vertices.Count)
                {
                    continue;
                }

                var first = group.Slot1Attributes[triangle.A];
                var second = group.Slot1Attributes[triangle.B];
                var third = group.Slot1Attributes[triangle.C];
                var x = Math.Clamp((first.X + second.X + third.X) / 3f, 0f, 1f);
                var y = Math.Clamp((first.Y + second.Y + third.Y) / 3f, 0f, 1f);
                var color = new RgbColor(
                    ToByte(x),
                    ToByte(y),
                    ToByte(1f - (x + y) / 2f));
                var a = Project(group.Vertices[triangle.A], centerX, centerY, scale, centerMeshX, centerMeshY);
                var b = Project(group.Vertices[triangle.B], centerX, centerY, scale, centerMeshX, centerMeshY);
                var c = Project(group.Vertices[triangle.C], centerX, centerY, scale, centerMeshX, centerMeshY);
                frame.FillTriangle(a.X, a.Y, b.X, b.Y, c.X, c.Y, color);
            }
        }

        return drawnGroups;
    }

    /// <summary>
    /// Draws a candidate mesh using the generic complete slot-1 float2 stream
    /// as normalized image coordinates. This is intentionally an opt-in
    /// diagnostic path for an already matched material asset; it does not
    /// establish that slot 1 is universally UV data or that the original GPU
    /// draw state has been decoded.
    /// </summary>
    public static int DrawSlot1TextureCandidate(
        SoftwareFrameBuffer frame,
        EaglCandidateMesh mesh,
        PcShapeImageTexture texture,
        int centerX,
        int centerY,
        int pixelSize)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(texture);
        if (pixelSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelSize));
        }

        var vertices = mesh.Groups.SelectMany(group => group.Vertices).ToArray();
        if (vertices.Length == 0)
        {
            return 0;
        }

        var minX = vertices.Min(vertex => vertex.X);
        var maxX = vertices.Max(vertex => vertex.X);
        var minY = vertices.Min(vertex => vertex.Y);
        var maxY = vertices.Max(vertex => vertex.Y);
        var spanX = Math.Max(0.0001f, maxX - minX);
        var spanY = Math.Max(0.0001f, maxY - minY);
        var scale = pixelSize / Math.Max(spanX, spanY);
        var centerMeshX = (minX + maxX) / 2f;
        var centerMeshY = (minY + maxY) / 2f;
        var drawnGroups = 0;

        foreach (var group in mesh.Groups)
        {
            if (group.Slot1Attributes.Count != group.Vertices.Count)
            {
                continue;
            }

            drawnGroups++;
            foreach (var triangle in group.Triangles)
            {
                if ((uint)triangle.A >= (uint)group.Vertices.Count ||
                    (uint)triangle.B >= (uint)group.Vertices.Count ||
                    (uint)triangle.C >= (uint)group.Vertices.Count)
                {
                    continue;
                }

                var a = Project(group.Vertices[triangle.A], centerX, centerY, scale, centerMeshX, centerMeshY);
                var b = Project(group.Vertices[triangle.B], centerX, centerY, scale, centerMeshX, centerMeshY);
                var c = Project(group.Vertices[triangle.C], centerX, centerY, scale, centerMeshX, centerMeshY);
                frame.FillTexturedTriangle(
                    a.X,
                    a.Y,
                    b.X,
                    b.Y,
                    c.X,
                    c.Y,
                    group.Slot1Attributes[triangle.A],
                    group.Slot1Attributes[triangle.B],
                    group.Slot1Attributes[triangle.C],
                    texture.DecodedRgba,
                    texture.Width,
                    texture.Height);
            }
        }

        return drawnGroups;
    }

    public static void DrawSolid(
        SoftwareFrameBuffer frame,
        EaglCandidateMesh mesh,
        int centerX,
        int centerY,
        int pixelSize,
        RgbColor color)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(mesh);
        if (pixelSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelSize));
        }

        var vertices = mesh.Groups.SelectMany(group => group.Vertices).ToArray();
        if (vertices.Length == 0)
        {
            return;
        }

        var minX = vertices.Min(vertex => vertex.X);
        var maxX = vertices.Max(vertex => vertex.X);
        var minY = vertices.Min(vertex => vertex.Y);
        var maxY = vertices.Max(vertex => vertex.Y);
        var spanX = Math.Max(0.0001f, maxX - minX);
        var spanY = Math.Max(0.0001f, maxY - minY);
        var scale = pixelSize / Math.Max(spanX, spanY);
        var centerMeshX = (minX + maxX) / 2f;
        var centerMeshY = (minY + maxY) / 2f;

        foreach (var group in mesh.Groups)
        {
            foreach (var triangle in group.Triangles)
            {
                if ((uint)triangle.A >= (uint)group.Vertices.Count ||
                    (uint)triangle.B >= (uint)group.Vertices.Count ||
                    (uint)triangle.C >= (uint)group.Vertices.Count)
                {
                    continue;
                }

                var a = Project(group.Vertices[triangle.A], centerX, centerY, scale, centerMeshX, centerMeshY);
                var b = Project(group.Vertices[triangle.B], centerX, centerY, scale, centerMeshX, centerMeshY);
                var c = Project(group.Vertices[triangle.C], centerX, centerY, scale, centerMeshX, centerMeshY);
                frame.FillTriangle(a.X, a.Y, b.X, b.Y, c.X, c.Y, color);
            }
        }
    }

    /// <summary>
    /// Draws the same candidate triangles in a selected Cartesian coordinate
    /// plane. This is an opt-in diagnostic comparison; it does not identify
    /// the game's camera, axis convention, vertex semantics or GPU transform.
    /// </summary>
    public static void DrawSolidProjection(
        SoftwareFrameBuffer frame,
        EaglCandidateMesh mesh,
        int centerX,
        int centerY,
        int pixelSize,
        RgbColor color,
        EaglCandidateProjectionPlane plane)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(mesh);
        if (pixelSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelSize));
        }

        var context = CreateProjectionContext(mesh, pixelSize, plane);
        if (context is null)
        {
            return;
        }

        foreach (var group in mesh.Groups)
        {
            foreach (var triangle in group.Triangles)
            {
                if ((uint)triangle.A >= (uint)group.Vertices.Count ||
                    (uint)triangle.B >= (uint)group.Vertices.Count ||
                    (uint)triangle.C >= (uint)group.Vertices.Count)
                {
                    continue;
                }

                var a = Project(
                    group.Vertices[triangle.A],
                    centerX,
                    centerY,
                    context.Value,
                    plane);
                var b = Project(
                    group.Vertices[triangle.B],
                    centerX,
                    centerY,
                    context.Value,
                    plane);
                var c = Project(
                    group.Vertices[triangle.C],
                    centerX,
                    centerY,
                    context.Value,
                    plane);
                frame.FillTriangle(a.X, a.Y, b.X, b.Y, c.X, c.Y, color);
            }
        }
    }

    public static void DrawWireframe(
        SoftwareFrameBuffer frame,
        EaglCandidateMesh mesh,
        int centerX,
        int centerY,
        int pixelSize,
        RgbColor color)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(mesh);
        if (pixelSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelSize));
        }

        var vertices = mesh.Groups.SelectMany(group => group.Vertices).ToArray();
        if (vertices.Length == 0)
        {
            return;
        }

        var minX = vertices.Min(vertex => vertex.X);
        var maxX = vertices.Max(vertex => vertex.X);
        var minY = vertices.Min(vertex => vertex.Y);
        var maxY = vertices.Max(vertex => vertex.Y);
        var spanX = Math.Max(0.0001f, maxX - minX);
        var spanY = Math.Max(0.0001f, maxY - minY);
        var scale = pixelSize / Math.Max(spanX, spanY);
        var centerMeshX = (minX + maxX) / 2f;
        var centerMeshY = (minY + maxY) / 2f;

        foreach (var group in mesh.Groups)
        {
            foreach (var triangle in group.Triangles)
            {
                if ((uint)triangle.A >= (uint)group.Vertices.Count ||
                    (uint)triangle.B >= (uint)group.Vertices.Count ||
                    (uint)triangle.C >= (uint)group.Vertices.Count)
                {
                    continue;
                }

                var a = Project(group.Vertices[triangle.A], centerX, centerY, scale, centerMeshX, centerMeshY);
                var b = Project(group.Vertices[triangle.B], centerX, centerY, scale, centerMeshX, centerMeshY);
                var c = Project(group.Vertices[triangle.C], centerX, centerY, scale, centerMeshX, centerMeshY);
                frame.DrawLine(a.X, a.Y, b.X, b.Y, color, 1);
                frame.DrawLine(b.X, b.Y, c.X, c.Y, color, 1);
                frame.DrawLine(c.X, c.Y, a.X, a.Y, color, 1);
            }
        }
    }

    /// <summary>
    /// Draws candidate triangle edges in a selected Cartesian coordinate
    /// plane. The output is a geometry diagnostic only; no axis or camera
    /// semantics are promoted from the comparison.
    /// </summary>
    public static void DrawWireframeProjection(
        SoftwareFrameBuffer frame,
        EaglCandidateMesh mesh,
        int centerX,
        int centerY,
        int pixelSize,
        RgbColor color,
        EaglCandidateProjectionPlane plane)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(mesh);
        if (pixelSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelSize));
        }

        var context = CreateProjectionContext(mesh, pixelSize, plane);
        if (context is null)
        {
            return;
        }

        foreach (var group in mesh.Groups)
        {
            foreach (var triangle in group.Triangles)
            {
                if ((uint)triangle.A >= (uint)group.Vertices.Count ||
                    (uint)triangle.B >= (uint)group.Vertices.Count ||
                    (uint)triangle.C >= (uint)group.Vertices.Count)
                {
                    continue;
                }

                var a = Project(
                    group.Vertices[triangle.A],
                    centerX,
                    centerY,
                    context.Value,
                    plane);
                var b = Project(
                    group.Vertices[triangle.B],
                    centerX,
                    centerY,
                    context.Value,
                    plane);
                var c = Project(
                    group.Vertices[triangle.C],
                    centerX,
                    centerY,
                    context.Value,
                    plane);
                frame.DrawLine(a.X, a.Y, b.X, b.Y, color, 1);
                frame.DrawLine(b.X, b.Y, c.X, c.Y, color, 1);
                frame.DrawLine(c.X, c.Y, a.X, a.Y, color, 1);
            }
        }
    }

    private static (int X, int Y) Project(
        EaglMeshVertex vertex,
        int centerX,
        int centerY,
        float scale,
        float centerMeshX,
        float centerMeshY) =>
        (
            centerX + (int)MathF.Round((vertex.X - centerMeshX) * scale),
            centerY - (int)MathF.Round((vertex.Y - centerMeshY) * scale));

    private static (int X, int Y) Project(
        EaglMeshVertex vertex,
        int centerX,
        int centerY,
        ProjectionContext context,
        EaglCandidateProjectionPlane plane)
    {
        var coordinates = GetPlaneCoordinates(vertex, plane);
        return
        (
            centerX + (int)MathF.Round((coordinates.First - context.CenterFirst) * context.Scale),
            centerY - (int)MathF.Round((coordinates.Second - context.CenterSecond) * context.Scale));
    }

    private static ProjectionContext? CreateProjectionContext(
        EaglCandidateMesh mesh,
        int pixelSize,
        EaglCandidateProjectionPlane plane)
    {
        var coordinates = mesh.Groups
            .SelectMany(group => group.Vertices)
            .Where(vertex =>
                float.IsFinite(vertex.X) &&
                float.IsFinite(vertex.Y) &&
                float.IsFinite(vertex.Z))
            .Select(vertex => GetPlaneCoordinates(vertex, plane))
            .ToArray();
        if (coordinates.Length == 0)
        {
            return null;
        }

        var minFirst = coordinates.Min(value => value.First);
        var maxFirst = coordinates.Max(value => value.First);
        var minSecond = coordinates.Min(value => value.Second);
        var maxSecond = coordinates.Max(value => value.Second);
        var spanFirst = Math.Max(0.0001f, maxFirst - minFirst);
        var spanSecond = Math.Max(0.0001f, maxSecond - minSecond);
        return new ProjectionContext(
            pixelSize / Math.Max(spanFirst, spanSecond),
            (minFirst + maxFirst) / 2f,
            (minSecond + maxSecond) / 2f);
    }

    private static (float First, float Second) GetPlaneCoordinates(
        EaglMeshVertex vertex,
        EaglCandidateProjectionPlane plane) =>
        plane switch
        {
            EaglCandidateProjectionPlane.XY => (vertex.X, vertex.Y),
            EaglCandidateProjectionPlane.XZ => (vertex.X, vertex.Z),
            EaglCandidateProjectionPlane.YZ => (vertex.Y, vertex.Z),
            _ => throw new ArgumentOutOfRangeException(nameof(plane)),
        };

    private readonly record struct ProjectionContext(
        float Scale,
        float CenterFirst,
        float CenterSecond);

    private static byte ToByte(float value) => (byte)Math.Clamp(MathF.Round(value * 255f), 0f, 255f);
}
