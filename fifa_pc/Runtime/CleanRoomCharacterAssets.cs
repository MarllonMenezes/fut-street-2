namespace FifaStreetPc;

/// <summary>
/// Authored character-asset manifest for the managed clean-room renderer. The
/// identifiers describe PC-owned silhouette/palette assets only; they are not
/// extracted mesh, material, UV, rigging, skinning, morph or GPU bindings.
/// </summary>
public readonly record struct CleanRoomCharacterAssetManifest(
    string SilhouetteAsset,
    string PaletteAsset)
{
    public static CleanRoomCharacterAssetManifest Default { get; } = new(
        "clean-room-character-silhouette-v1",
        "clean-room-character-palette-v1");

    public string ManifestId => "clean-room-character-manifest-v1";

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(SilhouetteAsset) &&
        !string.IsNullOrWhiteSpace(PaletteAsset);
}
