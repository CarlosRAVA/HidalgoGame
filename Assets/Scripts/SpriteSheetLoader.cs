using UnityEngine;

/// <summary>
/// Carga automáticamente todos los sprites de un Texture2D sliceado
/// y los asigna al SpriteSheetAnimator en el mismo GameObject.
/// Coloca este script junto con SpriteSheetAnimator en el prop animado.
/// </summary>
[RequireComponent(typeof(SpriteSheetAnimator))]
public class SpriteSheetLoader : MonoBehaviour
{
    [Tooltip("Sprite de cualquier frame del sheet (Unity cargará todos los del mismo sheet)")]
    public Sprite anyFrameFromSheet;

    void Awake()
    {
        if (anyFrameFromSheet == null) return;

        // Cargar todos los sprites que pertenecen al mismo texture
        Sprite[] allSprites = Resources.LoadAll<Sprite>(GetResourcePath(anyFrameFromSheet.texture.name));

        // Si no se encontró por Resources, carga a partir del mismo objeto
        if (allSprites == null || allSprites.Length == 0)
        {
#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(anyFrameFromSheet);
            Object[] objs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            System.Collections.Generic.List<Sprite> sprites = new System.Collections.Generic.List<Sprite>();
            foreach (Object obj in objs)
            {
                if (obj is Sprite s)
                    sprites.Add(s);
            }
            allSprites = sprites.ToArray();
#endif
        }

        if (allSprites != null && allSprites.Length > 0)
        {
            // Ordenar por nombre para asegurar secuencia correcta
            System.Array.Sort(allSprites, (a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));

            SpriteSheetAnimator anim = GetComponent<SpriteSheetAnimator>();
            anim.frames = allSprites;
        }
    }

    private string GetResourcePath(string textureName)
    {
        return textureName;
    }
}
