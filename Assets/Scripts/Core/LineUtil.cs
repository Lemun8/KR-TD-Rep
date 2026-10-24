using UnityEngine;

// Small helper for drawing lines and circles at runtime (road, range rings, markers)
public static class LineUtil
{
    private static Material sharedMaterial;

    private static Material GetMaterial()
    {
        if (sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");

            sharedMaterial = new Material(shader);
        }

        return sharedMaterial;
    }

    public static LineRenderer Create(Transform parent, string objectName, float width, Color color, int sortingOrder)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(parent, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = GetMaterial();
        line.useWorldSpace = true;
        line.widthMultiplier = width;
        line.numCornerVertices = 6;
        line.numCapVertices = 6;
        line.sortingOrder = sortingOrder;
        line.positionCount = 0;

        SetColor(line, color);
        return line;
    }

    public static void SetColor(LineRenderer line, Color color)
    {
        line.startColor = color;
        line.endColor = color;
    }

    public static void SetCircle(LineRenderer line, Vector3 center, float radius, int segments = 48)
    {
        line.loop = true;
        line.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            line.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
    }
}