using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class StarGenerator : MonoBehaviour
{
    [Header("Star Settings")]
    [Min(1)]
    public int starCount = 10;

    [Min(0f)]
    public float radius = 5f;

    [Min(0.01f)]
    public float starScale = 0.5f;

    public Sprite starSprite;
    public Material starMaterial;

    private const string containerName = "StarContainer";

    // Keep track of changes
    private int lastStarCount;
    private float lastRadius;
    private float lastScale;
    private Sprite lastSprite;
    private Material lastMaterial;

    private void OnEnable()
    {
        GenerateStars();
    }

    private void OnValidate()
    {
        if (starCount != lastStarCount ||
            radius != lastRadius ||
            starScale != lastScale ||
            starSprite != lastSprite ||
            starMaterial != lastMaterial)
        {
            GenerateStars();
        }
    }

    void GenerateStars()
    {
        Transform container = GetOrCreateContainer();

        ClearStars(container);

        for (int i = 0; i < starCount; i++)
        {
            Vector3 pos = Random.onUnitSphere * radius;

            GameObject star = new GameObject($"Star_{i}");
            star.transform.SetParent(container, false);
            star.transform.localPosition = pos;
            star.transform.localScale = Vector3.one * starScale;
            Vector3 direction = star.transform.position - transform.position;
            star.transform.rotation = Quaternion.LookRotation(direction);
            star.layer = LayerMask.NameToLayer("Stars");

            SpriteRenderer sr = star.AddComponent<SpriteRenderer>();
            sr.sprite = starSprite;

            if (starMaterial != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    sr.sharedMaterial = starMaterial;
                else
                    sr.material = starMaterial;
#else
                sr.material = starMaterial;
#endif
            }
        }

        lastStarCount = starCount;
        lastRadius = radius;
        lastScale = starScale;
        lastSprite = starSprite;
        lastMaterial = starMaterial;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    Transform GetOrCreateContainer()
    {
        Transform container = transform.Find(containerName);

        if (container == null)
        {
            GameObject newContainer = new GameObject(containerName);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.RegisterCreatedObjectUndo(newContainer, "Create Star Container");
#endif
            newContainer.transform.SetParent(transform, false);
            container = newContainer.transform;
        }

        return container;
    }

    void ClearStars(Transform container)
    {
        if (container == null)
            return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(child.gameObject);
            else
                Destroy(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }
    }
}
