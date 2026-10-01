using UnityEngine;

public class WorkspaceBoundaryVisualizer : MonoBehaviour
{
    [Header("Boundary Reference")]
    public Transform link_base; // xArm7'nin taban transform'u, Inspector'dan atanacak

    [Header("Boundary Shape")]
    public float radius = 0.7f; // xArm7'nin yaklaşık erişim yarıçapı (metre)
    public Color boundaryColor = Color.cyan;
    [Range(8, 64)]
    public int segments = 32;

    private GameObject boundaryObject;
    private LineRenderer[] circleRenderers;

    private void Awake()
    {
        BuildBoundaryVisual();
        HideBoundary();
    }

    private void BuildBoundaryVisual()
    {
        boundaryObject = new GameObject("BoundaryWireframe");
        boundaryObject.transform.SetParent(link_base != null ? link_base : transform, false);
        boundaryObject.transform.localPosition = Vector3.zero;

        // Küreyi 3 dik çember (XY, XZ, YZ) ile temsil ediyoruz - basit wireframe görünümü
        circleRenderers = new LineRenderer[3];
        circleRenderers[0] = CreateCircle("Circle_XY", Vector3.forward);
        circleRenderers[1] = CreateCircle("Circle_XZ", Vector3.up);
        circleRenderers[2] = CreateCircle("Circle_YZ", Vector3.right);
    }

    private LineRenderer CreateCircle(string name, Vector3 normal)
    {
        GameObject circleObj = new GameObject(name);
        circleObj.transform.SetParent(boundaryObject.transform, false);

        LineRenderer lr = circleObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.startWidth = 0.005f;
        lr.endWidth = 0.005f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = boundaryColor;
        lr.endColor = boundaryColor;

        lr.positionCount = segments;

        // Verilen normale dik bir çember çiz
        Vector3 forwardAxis = normal == Vector3.forward ? Vector3.right : Vector3.forward;
        Vector3 axisA = Vector3.Cross(normal, forwardAxis).normalized;
        Vector3 axisB = Vector3.Cross(normal, axisA).normalized;

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 point = (axisA * Mathf.Cos(angle) + axisB * Mathf.Sin(angle)) * radius;
            lr.SetPosition(i, point);
        }

        return lr;
    }

    public void ShowBoundary()
    {
        if (boundaryObject != null)
            boundaryObject.SetActive(true);
    }

    public void HideBoundary()
    {
        if (boundaryObject != null)
            boundaryObject.SetActive(false);
    }

    // Şu an boundary sabit ve renk değiştirmiyor, bu yüzden içi boş.
    // İleride el pozisyonuna göre bir davranış eklemek istersen buraya yazılır.
    public void UpdateHandPosition(Vector3 localHandPosition)
    {
    }
}
