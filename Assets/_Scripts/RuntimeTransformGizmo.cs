using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RuntimeTransformGizmo : MonoBehaviour
{
    public enum GizmoMode { Translate, Rotate, Scale }
    public enum RotateStyle { Ring, AxisDrag }

    #region Inspector
    [Header("Modo")]
    public GizmoMode mode = GizmoMode.Translate;
    public RotateStyle rotateStyle = RotateStyle.Ring;

    [Header("Visual – Translate (setas)")]
    public float axisLength = 1.2f;
    public float axisRadius = 0.05f;
    public float coneLength = 0.30f;
    public float coneRadius = 0.10f;

    [Header("Visual – Rotate (rings)")]
    public float ringRadius = 1.0f;
    public float ringThickness = 0.06f;
    public int ringSegments = 48;

    [Header("Comportamento")]
    public float dragSensitivity = 1f;
    public float rotateSpeed = 120f;
    public bool spaceIsGlobal = true;

    [Header("Raycast")]
    public string gizmoLayerName = "Gizmo";
    public bool ignoreUI = true;

    [Header("Refs")]
    public Camera cam;

    [Header("Materiais (opcional – arraste via Inspector)")]
    public Material axisMatTemplate; // ex.: URP/Lit
    public Material ringMatTemplate; // ex.: URP/Unlit

    [Header("Tamanho em tela")]
    public bool keepConstantScreenSize = true;
    [Range(40, 300)] public float targetScreenSizePx = 120f; // altura desejada em pixels


    [Header("Debug")]
    public bool debugLogs = false;
    #endregion

    #region Estado interno
    Transform target;
    Handle activeHandle;

    Plane dragPlane;
    Vector3 dragStartWorld;
    Vector3 targetStartPos;
    Vector3 targetStartScale;
    Quaternion targetStartRot;
    Vector2 lastMouse;
    Vector3 ringStartDir;

    GizmoMode _lastShownMode;

    readonly List<Handle> handles = new();
    int gizmoLayer;
    #endregion

    #region Tipos internos
    class Handle
    {
        public Transform root;
        public Axis axis;
        public bool isRing;
        public bool isScale;
        public readonly List<Collider> colliders = new();
        public Vector3 axisWorld;
        public Material mat;
    }

    enum Axis { X, Y, Z }
    #endregion

    #region Lifecycle
    void Awake()
    {
        EnsureCamera();

        gizmoLayer = LayerMask.NameToLayer(gizmoLayerName);
        if (gizmoLayer < 0)
        {
            if (debugLogs) Debug.LogWarning($"[Gizmo] Layer '{gizmoLayerName}' não existe. Usando 'Default'.");
            gizmoLayer = 0;
        }

        BuildHandles();
        ApplyLayerRecursive(transform, gizmoLayer);

        _lastShownMode = mode;
        SetVisible(false); // começa oculto até anexar um alvo
    }

    void Update()
    {
        if (Keyboard.current != null && mode == GizmoMode.Rotate)
        {
            if (Keyboard.current.yKey.wasPressedThisFrame)
                rotateStyle = (rotateStyle == RotateStyle.Ring ? RotateStyle.AxisDrag : RotateStyle.Ring);
        }

        if (mode != _lastShownMode)
        {
            _lastShownMode = mode;
            activeHandle = null;
            ApplyModeVisibility();
        }

        if (!target) return;

        transform.position = target.position;
        transform.rotation = spaceIsGlobal ? Quaternion.identity : target.rotation;

        if (keepConstantScreenSize && cam && target)
        {
            float scale = ComputeScreenConstantScale(transform.position, targetScreenSizePx);
            transform.localScale = Vector3.one * scale;
        }

        UpdateAxisWorld();

        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryBeginDrag();

        if (Mouse.current.leftButton.isPressed && activeHandle != null)
            DoDrag();

        if (Mouse.current.leftButton.wasReleasedThisFrame && activeHandle != null)
            EndDrag();
    }
    #endregion

    #region API
    public void Attach(Transform t)
    {
        target = t;
        transform.position = t.position;
        ApplyModeVisibility();
    }

    public void Detach()
    {
        target = null;
        SetVisible(false);
        activeHandle = null;
    }
    #endregion

    #region Drag Flow
    void TryBeginDrag()
    {
        if (!ignoreUI && IsPointerOverUI())
        {
            if (debugLogs) Debug.Log("[Gizmo] Clique bloqueado pela UI.");
            return;
        }

        if (!RaycastHandle(out var h))
        {
            if (debugLogs) Debug.Log("[Gizmo] Nenhuma alça sob o cursor.");
            return;
        }

        activeHandle = h;
        targetStartPos = target.position;
        targetStartRot = target.rotation;
        if (mode == GizmoMode.Scale) targetStartScale = target.localScale;

        // Translate, Rotate (AxisDrag pelas setas) OU Scale (eixos com cubo)
        if (mode == GizmoMode.Translate ||
            (mode == GizmoMode.Rotate && rotateStyle == RotateStyle.AxisDrag && !h.isRing) ||
            (mode == GizmoMode.Scale))
        {
            var n1 = cam.transform.forward; // plano da tela
            var camDir = (cam.transform.position - transform.position).normalized;
            var n2 = Vector3.Cross(h.axisWorld, camDir).normalized; // ortogonal eixo/câmera
            var n3 = Vector3.up;

            if (!TrySetDragPlane(n1) && !TrySetDragPlane(n2) && !TrySetDragPlane(n3))
            {
                if (debugLogs) Debug.Log("[Gizmo] Falhou definir plano de arraste.");
                activeHandle = null;
                return;
            }

            if (RayToPlane(MouseRay(), dragPlane, out dragStartWorld))
            {
                lastMouse = Mouse.current.position.ReadValue();
                if (debugLogs)
                {
                    string label = mode == GizmoMode.Scale ? "Scale/Axes"
                                  : (mode == GizmoMode.Translate ? "Translate/Axes" : "Rotate AxisDrag");
                    Debug.Log($"[Gizmo] BeginDrag {label} ({h.axis})");
                }
            }
            else
            {
                if (debugLogs) Debug.Log("[Gizmo] Falhou calcular ponto inicial (AxisDrag/Scale).");
                activeHandle = null;
            }
            return;
        }

        // Rotate por anel (Ring)
        dragPlane = new Plane(h.axisWorld.normalized, transform.position);
        if (RayToPlane(MouseRay(), dragPlane, out var hit))
        {
            ringStartDir = (hit - transform.position);
            if (ringStartDir.sqrMagnitude < 1e-6f)
            {
                if (debugLogs) Debug.Log("[Gizmo] Vetor inicial muito pequeno no anel.");
                activeHandle = null;
                return;
            }
            ringStartDir.Normalize();
            if (debugLogs) Debug.Log($"[Gizmo] BeginDrag Ring ({h.axis})");
        }
        else
        {
            if (debugLogs) Debug.Log("[Gizmo] Falhou calcular ponto no plano do anel.");
            activeHandle = null;
        }

        bool TrySetDragPlane(Vector3 normal)
        {
            if (normal.sqrMagnitude < 1e-6f) return false;
            dragPlane = new Plane(normal.normalized, transform.position);
            var ray = MouseRay();
            var denom = Mathf.Abs(Vector3.Dot(ray.direction, dragPlane.normal));
            return denom > 1e-6f;
        }
    }


    void DoDrag()
    {
        if (activeHandle == null) return;

        // ========== SCALE ==========
        if (mode == GizmoMode.Scale && !activeHandle.isRing)
        {
            if (!RayToPlane(MouseRay(), dragPlane, out var hit)) return;

            var delta = hit - dragStartWorld;
            var axisWorld = activeHandle.axisWorld.normalized;

            // projeção do movimento do mouse no eixo ativo
            float mag = Vector3.Dot(delta, axisWorld) * dragSensitivity;

            // fator de escala (1 = sem mudança)
            // positivo aumenta; negativo diminui
            // dica: use valores pequenos de dragSensitivity pra não "explodir" a escala
            bool uniform = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
            const float MinScale = 0.01f; // anti zero/negativo

            if (uniform)
            {
                float f = Mathf.Max(MinScale, 1f + mag);
                var s = targetStartScale * f;
                s.x = Mathf.Max(MinScale, s.x);
                s.y = Mathf.Max(MinScale, s.y);
                s.z = Mathf.Max(MinScale, s.z);
                target.localScale = s;
            }
            else
            {
                // escala por eixo
                var s = targetStartScale;
                float f = Mathf.Max(MinScale, 1f + mag);

                switch (activeHandle.axis)
                {
                    case Axis.X: s.x = Mathf.Max(MinScale, targetStartScale.x * f); break;
                    case Axis.Y: s.y = Mathf.Max(MinScale, targetStartScale.y * f); break;
                    case Axis.Z: s.z = Mathf.Max(MinScale, targetStartScale.z * f); break;
                }
                target.localScale = s;
            }
            return;
        }

        // ========== TRANSLATE ou ROTATE por setas (AxisDrag) ==========
        if (mode == GizmoMode.Translate ||
            (mode == GizmoMode.Rotate && rotateStyle == RotateStyle.AxisDrag && !activeHandle.isRing))
        {
            if (!RayToPlane(MouseRay(), dragPlane, out var hit)) return;

            var delta = hit - dragStartWorld;
            var axis = activeHandle.axisWorld.normalized;

            var mag = Vector3.Dot(delta, axis) * dragSensitivity;
            target.position = targetStartPos + axis * mag;

            if (mode == GizmoMode.Rotate && rotateStyle == RotateStyle.AxisDrag)
            {
                var cur = Mouse.current.position.ReadValue();
                var mouseDelta = cur - lastMouse;
                lastMouse = cur;

                var sign = Vector3.Dot(axis, (transform.position - cam.transform.position).normalized) > 0 ? -1f : 1f;
                var angle = sign * mouseDelta.x * (rotateSpeed / 200f);
                target.rotation = Quaternion.AngleAxis(angle, axis) * target.rotation;
            }
            return;
        }

        // ========== ROTATE por anel (Ring) ==========
        if (!RayToPlane(MouseRay(), dragPlane, out var hitRing)) return;

        var curDir = (hitRing - transform.position);
        if (curDir.sqrMagnitude < 1e-6f) return;
        curDir.Normalize();

        var rotAxis = activeHandle.axisWorld.normalized;
        var signedAngle = Vector3.SignedAngle(ringStartDir, curDir, rotAxis);

        target.rotation = Quaternion.AngleAxis(signedAngle, rotAxis) * targetStartRot;
    }


    void EndDrag()
    {
        activeHandle = null;
        if (debugLogs) Debug.Log("[Gizmo] EndDrag");
    }
    #endregion

    #region Raycast helpers
    bool RaycastHandle(out Handle h)
    {
        h = null;

        EnsureCamera();
        if (!cam) return false;

        var ray = MouseRay();
        var mask = 1 << gizmoLayer;

        const float pickRadius = 0.08f;
        var hits = Physics.SphereCastAll(ray, pickRadius, 2000f, mask, QueryTriggerInteraction.Collide);

        if (hits.Length == 0 && gizmoLayer == 0)
            hits = Physics.SphereCastAll(ray, pickRadius, 2000f, ~0, QueryTriggerInteraction.Collide);

        if (hits.Length == 0) return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            var t = hit.collider.transform;
            while (t != null && t != transform && !t.name.StartsWith("Handle_"))
                t = t.parent;

            if (t == null || t == transform) continue;

            foreach (var hh in handles)
            {
                if (t == hh.root)
                {
                    h = hh;
                    return true;
                }
            }
        }
        return false;
    }

    Ray MouseRay()
    {
        EnsureCamera();
        var mp = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : new Vector2(Screen.width / 2f, Screen.height / 2f);

        return cam.ScreenPointToRay(mp);
    }

    static bool RayToPlane(Ray r, Plane p, out Vector3 world)
    {
        if (p.Raycast(r, out var d))
        {
            world = r.GetPoint(d);
            return true;
        }
        world = default;
        return false;
    }
    #endregion

    #region Build gizmos
    void BuildHandles()
    {
        // Setas (Translate / AxisDrag)
        CreateAxis(Axis.X, Color.red, Quaternion.FromToRotation(Vector3.up, Vector3.right));
        CreateAxis(Axis.Y, Color.green, Quaternion.identity);
        CreateAxis(Axis.Z, Color.blue, Quaternion.FromToRotation(Vector3.up, Vector3.forward));

        // Anéis (Rotate)
        CreateRing(Axis.X, Color.red, Quaternion.FromToRotation(Vector3.up, Vector3.right));
        CreateRing(Axis.Y, Color.green, Quaternion.identity);
        CreateRing(Axis.Z, Color.blue, Quaternion.FromToRotation(Vector3.up, Vector3.forward));

        // Scale (cubos)
        CreateScaleAxis(Axis.X, Color.yellow, Quaternion.FromToRotation(Vector3.up, Vector3.right));
        CreateScaleAxis(Axis.Y, Color.yellow, Quaternion.identity);
        CreateScaleAxis(Axis.Z, Color.yellow, Quaternion.FromToRotation(Vector3.up, Vector3.forward));
    }

    void CreateScaleAxis(Axis axis, Color color, Quaternion rot)
    {
        var root = new GameObject("Handle_" + axis + "_Scale").transform;
        root.SetParent(transform, false);
        root.localRotation = rot;

        // Haste
        var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Shaft";
        shaft.transform.SetParent(root, false);
        shaft.transform.localScale = new Vector3(axisRadius, axisLength * 0.5f, axisRadius);
        shaft.transform.localPosition = new Vector3(0f, axisLength * 0.5f, 0f);

        // Cubo na ponta (indicador de scale)
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Cube";
        cube.transform.SetParent(root, false);
        cube.transform.localScale = Vector3.one * coneRadius * 2f;
        cube.transform.localPosition = new Vector3(0f, axisLength + (coneRadius), 0f);

        var mat = axisMatTemplate ? new Material(axisMatTemplate)
                                  : SafeMaterial("Universal Render Pipeline/Lit", color);
        shaft.GetComponent<MeshRenderer>().sharedMaterial = mat;
        cube.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var envelope = root.gameObject.AddComponent<BoxCollider>();
        envelope.isTrigger = true;
        envelope.center = new Vector3(0f, axisLength * 0.5f, 0f);
        envelope.size = new Vector3(coneRadius * 2.5f, axisLength + coneRadius * 2f, coneRadius * 2.5f);

        var h = new Handle { root = root, axis = axis, mat = mat, isRing = false, isScale = true };
        h.colliders.Add(shaft.GetComponent<Collider>());
        h.colliders.Add(cube.GetComponent<Collider>());
        h.colliders.Add(envelope);
        handles.Add(h);
    }


    void CreateAxis(Axis axis, Color color, Quaternion rot)
    {
        var root = new GameObject("Handle_" + axis).transform;
        root.SetParent(transform, false);
        root.localRotation = rot;

        // Haste
        var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Shaft";
        shaft.transform.SetParent(root, false);
        shaft.transform.localScale = new Vector3(axisRadius, axisLength * 0.5f, axisRadius);
        shaft.transform.localPosition = new Vector3(0f, axisLength * 0.5f, 0f);

        // Ponta (Capsule para evitar SphereCollider stripping no WebGL)
        var tip = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        tip.name = "Tip";
        tip.transform.SetParent(root, false);
        tip.transform.localScale = new Vector3(coneRadius, coneLength * 0.5f, coneRadius);
        tip.transform.localPosition = new Vector3(0f, axisLength + coneLength * 0.6f, 0f);

        // Materiais (seguro com fallback)
        var mat = axisMatTemplate ? new Material(axisMatTemplate)
                                  : SafeMaterial("Universal Render Pipeline/Lit", color);
        shaft.GetComponent<MeshRenderer>().sharedMaterial = mat;
        tip.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // Envelope para clique
        var envelope = root.gameObject.AddComponent<BoxCollider>();
        envelope.isTrigger = true;
        envelope.center = new Vector3(0f, axisLength * 0.5f, 0f);
        envelope.size = new Vector3(coneRadius * 2.5f, axisLength + coneLength * 1.2f, coneRadius * 2.5f);

        // Registrar handle
        var h = new Handle { root = root, axis = axis, mat = mat, isRing = false };
        h.colliders.Add(shaft.GetComponent<Collider>());
        h.colliders.Add(tip.GetComponent<Collider>());
        h.colliders.Add(envelope);
        handles.Add(h);
    }

    void CreateRing(Axis axis, Color color, Quaternion rot)
    {
        var root = new GameObject("Handle_" + axis + "_Ring").transform;
        root.SetParent(transform, false);
        root.localRotation = rot;

        // Visual
        var lr = root.gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = ringSegments;
        lr.widthMultiplier = ringThickness * 0.35f;

        // material seguro (template ou fallback)
        lr.material = ringMatTemplate ? new Material(ringMatTemplate)
                                      : SafeMaterial("Universal Render Pipeline/Unlit", color);

        for (int i = 0; i < ringSegments; i++)
        {
            var t = (i / (float)ringSegments) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(t) * ringRadius, 0f, Mathf.Sin(t) * ringRadius));
        }

        // Colliders segmentados
        var arcLen = 2f * Mathf.PI * ringRadius / ringSegments;
        for (int i = 0; i < ringSegments; i++)
        {
            var t = (i / (float)ringSegments) * Mathf.PI * 2f;
            var nx = Mathf.Cos(t) * ringRadius;
            var nz = Mathf.Sin(t) * ringRadius;

            var seg = new GameObject("Seg_" + i);
            seg.transform.SetParent(root, false);
            seg.transform.localPosition = new Vector3(nx, 0f, nz);

            var tangent = new Vector3(-Mathf.Sin(t), 0f, Mathf.Cos(t));
            seg.transform.localRotation = Quaternion.LookRotation(tangent, Vector3.up);

            var bc = seg.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(ringThickness, ringThickness, arcLen * 1.2f);
        }

        // Registrar handle
        var mat = axisMatTemplate ? new Material(axisMatTemplate)
                                  : SafeMaterial("Universal Render Pipeline/Lit", color);

        var h = new Handle { root = root, axis = axis, mat = mat, isRing = true };
        foreach (var bc in root.GetComponentsInChildren<BoxCollider>()) h.colliders.Add(bc);
        handles.Add(h);
    }
    #endregion

    #region Utils
    static Vector3 AxisToVector(Axis a) => a switch
    {
        Axis.X => Vector3.right,
        Axis.Y => Vector3.up,
        Axis.Z => Vector3.forward,
        _ => Vector3.right
    };

    void UpdateAxisWorld()
    {
        foreach (var h in handles)
            h.axisWorld = spaceIsGlobal ? AxisToVector(h.axis) : (target.rotation * AxisToVector(h.axis));
    }

    float ComputeScreenConstantScale(Vector3 worldPos, float sizePx)
    {
        // Distância da câmera até o gizmo
        float d = Vector3.Distance(cam.transform.position, worldPos);

        // Altura visível em world units para "sizePx" pixels na tela
        float worldHeight = 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * (sizePx / Screen.height);

        return worldHeight;
    }

    void ApplyModeVisibility()
    {
        if (!target) { SetVisible(false); return; }

        bool showRings = (mode == GizmoMode.Rotate);
        bool showArrows = (mode == GizmoMode.Translate);
        bool showScales = (mode == GizmoMode.Scale);

        foreach (var h in handles)
        {
            bool on =
                (h.isRing && showRings) ||                 // anéis para Rotate
                (!h.isRing && !h.isScale && showArrows) || // setas para Translate
                (h.isScale && showScales);                 // cubos para Scale

            h.root.gameObject.SetActive(on);
        }
    }

    void SetVisible(bool v)
    {
        foreach (var h in handles) h.root.gameObject.SetActive(v);
    }

    void ApplyLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++)
            ApplyLayerRecursive(t.GetChild(i), layer);
    }

    void EnsureCamera()
    {
        if (!cam) cam = Camera.main;
    }

    static bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null &&
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

    static Material SafeMaterial(string shaderName, Color c)
    {
        var sh = Shader.Find(shaderName);
        if (sh == null)
        {
            Debug.LogWarning($"[Gizmo] Shader '{shaderName}' não encontrado. Usando fallback 'Sprites/Default'.");
            sh = Shader.Find("Sprites/Default");
        }
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        return m;
    }
    #endregion
}
