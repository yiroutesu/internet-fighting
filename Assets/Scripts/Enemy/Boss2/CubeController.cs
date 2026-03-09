using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;

public class CubeController : MonoBehaviour
{
    #region 变量
    [Header("角块配置")]
    public GameObject FLU;
    public GameObject FRU;
    public GameObject BLU;
    public GameObject BRU;
    public GameObject FLD;
    public GameObject FRD;
    public GameObject BLD;
    public GameObject BRD;
    private GameObject[] cornerPieces = new GameObject[8];
    private Vector3[] cornerPositions = new Vector3[8];

    [Header("转动父级")]
    [Tooltip("用于执行转动的临时父级对象")]
    public Transform rotationParent;
    public Transform rotationParentTwo;
    private Quaternion cubeSpaceNeutral;   // 魔方的"零位"朝向

    [Header("默认父级")]
    [Tooltip("所有角块的默认父级，当不转动时角块都在此父级下")]
    public Transform defaultParent;

    [Header("魔方设置")]
    [Tooltip("魔方边长的一半")]
    public float cubeHalfSize = 1f;
    [Tooltip("转动速度")]
    public float rotationSpeed = 300f;

    [Header("旋转动画曲线")]
    [Tooltip("控制旋转过程的缓动曲线")]
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private bool isRotating = false;
    private List<GameObject> currentRotatingPieces = new List<GameObject>();

    #region 随机位移效果
    [Header("随机位移设置")]
    [Tooltip("是否启用随机位移效果")]
    public bool enableRandomDisplacement = false;
    [Tooltip("位移幅度")]
    public float displacementMagnitude = 0.1f;
    [Tooltip("位移更新频率(秒)")]
    public float displacementUpdateInterval = 0.5f;
    [Tooltip("位移过渡平滑度")]
    [Range(0.1f, 5f)]
    public float displacementSmoothness = 2f;

    private float displacementTimer = 0f;
    private Vector3[] currentDisplacements = new Vector3[8];
    private Vector3[] targetDisplacements = new Vector3[8];
    private Vector3[] startDisplacements = new Vector3[8];
    private Dictionary<GameObject, Vector3> originalDisplacementPositions = new Dictionary<GameObject, Vector3>();
    #endregion

    #endregion
    #region 事件
    public UnityEvent RotateFaceOver = new UnityEvent();
    #endregion
    void Start()
    {
        if (defaultParent == null)
        {
            defaultParent = transform;
        }

        // 如果没有设置旋转曲线，使用默认的缓入缓出曲线
        if (rotationCurve == null || rotationCurve.keys.Length < 2)
        {
            rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }

        InitializeCube();
    }

    #region 动画启动协程
    // 启动协程：协调旋转动画和面层动画
    public void StartCombinedAnimation(Face face, float rotationAngle, float rotationSpeed,
                                      float layerDuration, Vector3 layerDirectionCoefficient,
                                      AnimationCurve layerCurve = null, float layerDelay = 0f)
    {
        if (isRotating) return;

        StartCoroutine(PerformCombinedAnimation(face, rotationAngle, rotationSpeed,
            layerDuration, layerDirectionCoefficient, layerCurve, layerDelay));
    }

    private IEnumerator PerformCombinedAnimation(Face face, float rotationAngle, float rotationSpeed,
                                                float layerDuration, Vector3 layerDirectionCoefficient,
                                                AnimationCurve layerCurve, float layerDelay)
    {
        if (layerCurve == null) layerCurve = rotationCurve;

        // 1. 获取旋转面的角块
        List<GameObject> facePieces = FindFace(face);
        if (facePieces.Count != 4)
        {
            Debug.LogWarning("该面的角块数量不正确: " + facePieces.Count);
            yield break;
        }

        isRotating = true;
        currentRotatingPieces = facePieces;

        // 2. 旋转动画设置父级 - 修改：使用局部旋转
        rotationParent.position = transform.position;
        rotationParent.localRotation = Quaternion.identity;  // 重置为局部单位旋转
        cubeSpaceNeutral = Quaternion.identity;

        foreach (GameObject piece in facePieces)
        {
            piece.transform.SetParent(rotationParent);
        }

        // 3. 保存面层动画所需的局部原始位置
        Dictionary<GameObject, Vector3> originalLocalPositions = new Dictionary<GameObject, Vector3>();
        Dictionary<GameObject, Vector3> originalDirections = new Dictionary<GameObject, Vector3>();

        foreach (GameObject piece in facePieces)
        {
            Vector3 localPos = piece.transform.localPosition;
            originalLocalPositions[piece] = localPos;

            // 计算移动方向：角块局部坐标的标准化方向与系数相乘
            Vector3 direction = localPos.normalized;
            direction.Scale(layerDirectionCoefficient); // 使用系数调整方向
            originalDirections[piece] = direction;
        }

        // 4. 启动两个协程
        Coroutine rotationCoroutine = null;
        Coroutine layerCoroutine = null;

        // 启动旋转协程
        if (Mathf.Abs(rotationAngle) > 0.1f)
        {
            rotationCoroutine = StartCoroutine(PerformRotationOnly(face, rotationAngle, rotationSpeed));
        }

        // 启动面层动画协程（带延迟）
        if (layerDuration > 0 && layerDirectionCoefficient.magnitude > 0.01f)
        {
            layerCoroutine = StartCoroutine(PerformLayerAnimationOnly(facePieces, originalDirections,
                layerDuration, layerCurve, layerDelay));
        }

        // 等待两个协程完成
        if (rotationCoroutine != null) yield return rotationCoroutine;
        if (layerCoroutine != null) yield return layerCoroutine;

        // 5. 确保角块在局部坐标上返回原始位置
        foreach (GameObject piece in facePieces)
        {
            piece.transform.localPosition = originalLocalPositions[piece];
        }

        // 6. 角块脱离父级
        foreach (GameObject piece in facePieces)
        {
            piece.transform.SetParent(defaultParent);
        }

        // 7. 更新面的角块列表
        UpdateFaceLists();

        isRotating = false;
        currentRotatingPieces.Clear();
        RotateFaceOver?.Invoke();
    }

    // 纯旋转协程（不包含设置/解除父级） - 修改：使用局部旋转
    private IEnumerator PerformRotationOnly(Face face, float angle, float speed)
    {
        Vector3 axis = GetRotationAxis(face);
        Quaternion startRotation = rotationParent.localRotation;
        Quaternion targetRotation = Quaternion.AngleAxis(angle, axis) * startRotation;

        float duration = Mathf.Abs(angle) / speed;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float easedProgress = rotationCurve.Evaluate(progress);

            rotationParent.localRotation = Quaternion.Slerp(startRotation, targetRotation, easedProgress);
            yield return null;
        }

        rotationParent.localRotation = targetRotation;
    }

    // 纯面层动画协程（在临时父级中使用局部坐标）
    private IEnumerator PerformLayerAnimationOnly(List<GameObject> pieces,
                                                 Dictionary<GameObject, Vector3> directions,
                                                 float duration, AnimationCurve curve, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        // 保存原始局部位置
        Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
        foreach (GameObject piece in pieces)
        {
            originalPositions[piece] = piece.transform.localPosition;
        }

        float elapsedTime = 0f;

        // 向前移动阶段
        while (elapsedTime < duration / 2)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / (duration / 2);
            float easedProgress = curve.Evaluate(progress);

            foreach (GameObject piece in pieces)
            {
                Vector3 startPos = originalPositions[piece];
                Vector3 targetOffset = directions[piece];
                piece.transform.localPosition = Vector3.Lerp(startPos, startPos + targetOffset, easedProgress);
            }

            yield return null;
        }

        // 确保到达目标位置
        foreach (GameObject piece in pieces)
        {
            Vector3 targetOffset = directions[piece];
            piece.transform.localPosition = originalPositions[piece] + targetOffset;
        }

        // 等待短暂停留
        yield return new WaitForSeconds(0.1f);

        elapsedTime = 0f;

        // 返回阶段
        while (elapsedTime < duration / 2)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / (duration / 2);
            float easedProgress = curve.Evaluate(progress);

            foreach (GameObject piece in pieces)
            {
                Vector3 startPos = originalPositions[piece] + directions[piece];
                piece.transform.localPosition = Vector3.Lerp(startPos, originalPositions[piece], easedProgress);
            }

            yield return null;
        }

        // 确保返回原始位置（已在主协程中处理）
    }
    #endregion

    #region 单层旋转方法（保持原有接口）
    // 原始方法：使用枚举和默认速度
    public void RotateFace(Face face, RotationDirection direction)
    {
        RotateFace(face, direction, rotationSpeed);
    }

    // 重载1：使用枚举和自定义速度，角度默认为90度
    public void RotateFace(Face face, RotationDirection direction, float speed)
    {
        float angle = (direction == RotationDirection.Clockwise) ? -90f : 90f;
        RotateFace(face, angle, speed);
    }

    // 重载2：使用枚举、自定义角度和自定义速度
    public void RotateFace(Face face, RotationDirection direction, float angle, float speed)
    {
        float signedAngle = (direction == RotationDirection.Clockwise) ? -angle : angle;
        RotateFace(face, signedAngle, speed);
    }

    // 重载3：使用枚举、直接角度和自定义速度（角度正负决定方向）
    public void RotateFace(Face face, float angle, float speed)
    {
        if (isRotating || rotationParent == null || defaultParent == null) return;

        List<GameObject> facePieces = FindFace(face);
        if (facePieces.Count != 4)
        {
            Debug.LogWarning("该面的角块数量不正确: " + facePieces.Count);
            return;
        }
        angle = Mathf.Round(angle / 90) * 90;
        StartCoroutine(PerformRotationOld(facePieces, face, angle, speed));
    }

    // 原有旋转协程（保持兼容性） - 修改：使用局部旋转
    IEnumerator PerformRotationOld(List<GameObject> pieces, Face face, float angle, float speed)
    {
        isRotating = true;
        currentRotatingPieces = pieces;

        rotationParent.position = transform.position;
        rotationParent.localRotation = Quaternion.identity;  // 重置为局部单位旋转
        cubeSpaceNeutral = Quaternion.identity;

        foreach (GameObject piece in pieces)
        {
            piece.transform.SetParent(rotationParent);
        }

        Vector3 axis = GetRotationAxis(face);
        Quaternion targetRotation = Quaternion.AngleAxis(angle, axis) * rotationParent.localRotation;
        Quaternion startRotation = rotationParent.localRotation;

        float progress = 0f;
        float duration = Mathf.Abs(angle) / speed;
        float elapsedTime = 0f;

        while (progress < 1f)
        {
            elapsedTime += Time.deltaTime;
            progress = elapsedTime / duration;
            float easedProgress = rotationCurve.Evaluate(progress);

            rotationParent.localRotation = Quaternion.Slerp(startRotation, targetRotation, easedProgress);
            yield return null;
        }

        rotationParent.localRotation = targetRotation;

        foreach (GameObject piece in pieces)
        {
            piece.transform.SetParent(defaultParent);
        }

        UpdateFaceLists();
        isRotating = false;
        currentRotatingPieces.Clear();
        RotateFaceOver?.Invoke();
    }
    #endregion

    #region 对向双层旋转方法（改进版）

    /// <summary>
    /// 通过持续时间和旋转次数计算旋转速度
    /// </summary>
    /// <param name="face1">第一个旋转面</param>
    /// <param name="face2">第二个旋转面</param>
    /// <param name="rotations">旋转次数（正负代表方向）</param>
    /// <param name="duration">旋转总持续时间（秒）</param>
    public void RotateTwoFaceWithDuration(Face face1, Face face2, float angle, float duration)
    {
        Debug.Log("双城旋转");
        if (duration <= 0f)
        {
            Debug.LogWarning("持续时间必须大于0");
            return;
        }


        // 计算旋转速度（度/秒）
        float speed = Mathf.Abs(angle) / duration;

        RotateTwoFace(face1, face2, angle, speed);
    }

    /// <summary>
    /// 通过持续时间和旋转次数计算旋转速度（带默认旋转角度）
    /// </summary>
    /// <param name="face1">第一个旋转面</param>
    /// <param name="face2">第二个旋转面</param>
    /// <param name="rotations">旋转次数（正负代表方向）</param>
    /// <param name="duration">旋转总持续时间（秒）</param>
    /// <param name="baseAngle">每次旋转的基础角度（默认90度）</param>
    public void RotateTwoFaceWithDuration(Face face1, Face face2, int rotations, float duration, float baseAngle)
    {
        if (duration <= 0f)
        {
            Debug.LogWarning("持续时间必须大于0");
            return;
        }

        // 计算总角度
        float totalAngle = rotations * baseAngle;

        // 计算旋转速度（度/秒）
        float speed = Mathf.Abs(totalAngle) / duration;

        RotateTwoFace(face1, face2, totalAngle, speed);
    }

    /// <summary>
    /// 原始方法：直接指定角度和速度
    /// </summary>
    public void RotateTwoFace(Face face1, Face face2, float angle, float speed)
    {
        if (isRotating || rotationParent == null || rotationParentTwo == null || defaultParent == null)
        {
            Debug.LogWarning("无法旋转：正在进行旋转或父级对象未设置");
            return;
        }

        var pieces1 = FindFace(face1);
        var pieces2 = FindFace(face2);

        var overlap = pieces1.Intersect(pieces2).Any();
        if (overlap)
        {
            Debug.LogError($"面{face1}和面{face2}有重叠的角块，不能同时旋转");
            return;
        }

        StartCoroutine(PerformTwoFaceRotation(face1, face2, angle, speed));
    }

    /// <summary>
    /// 通过持续时间和旋转次数计算旋转速度，并返回协程
    /// </summary>
    public IEnumerator RotateTwoFaceWithDurationCoroutine(Face face1, Face face2, int rotations, float duration)
    {
        if (duration <= 0f)
        {
            Debug.LogWarning("持续时间必须大于0");
            yield break;
        }

        float totalAngle = rotations * 90f;
        float speed = Mathf.Abs(totalAngle) / duration;

        yield return RotateTwoFaceCoroutine(face1, face2, totalAngle, speed);
    }

    // 改进的双层旋转协程（保持不变）
    private IEnumerator PerformTwoFaceRotation(Face face1, Face face2, float angle, float speed)
    {
        Debug.Log("双城旋转协程");
        isRotating = true;

        var pieces1 = FindFace(face1);
        var pieces2 = FindFace(face2);

        rotationParent.position = transform.position;
        rotationParent.rotation = transform.rotation;
        foreach (GameObject piece in pieces1)
        {
            piece.transform.SetParent(rotationParent);
        }

        rotationParentTwo.position = transform.position;
        rotationParentTwo.rotation = transform.rotation;
        foreach (GameObject piece in pieces2)
        {
            piece.transform.SetParent(rotationParentTwo);
        }

        Vector3 axis1 = GetRotationAxis(face1);
        Vector3 axis2 = GetRotationAxis(face2);

        Quaternion target1 = Quaternion.AngleAxis(angle, axis1) * rotationParent.localRotation;
        Quaternion target2 = Quaternion.AngleAxis(angle, axis2) * rotationParentTwo.localRotation;

        Quaternion start1 = rotationParent.localRotation;
        Quaternion start2 = rotationParentTwo.localRotation;

        float progress = 0f;
        float duration = Mathf.Abs(angle) / speed;
        float elapsedTime = 0f;

        while (progress < 1f)
        {
            elapsedTime += Time.deltaTime;
            progress = elapsedTime / duration;
            float easedProgress = rotationCurve.Evaluate(progress);

            rotationParent.localRotation = Quaternion.Slerp(start1, target1, easedProgress);
            rotationParentTwo.localRotation = Quaternion.Slerp(start2, target2, easedProgress);
            yield return null;
        }

        rotationParent.localRotation = target1;
        rotationParentTwo.localRotation = target2;

        foreach (GameObject piece in pieces1.Concat(pieces2))
        {
            piece.transform.SetParent(defaultParent);
        }

        UpdateFaceLists();
        isRotating = false;
        RotateFaceOver?.Invoke();
    }

    // 新增：可等待的双层旋转方法（保持不变）
    public IEnumerator RotateTwoFaceAndWait(Face face1, Face face2, float angle, float speed)
    {
        bool rotationComplete = false;

        // 创建一个回调来标记旋转完成
        System.Action onRotationComplete = () =>
        {
            rotationComplete = true;
        };

        // 监听旋转完成事件
        RotateFaceOver.AddListener(() => onRotationComplete?.Invoke());

        // 执行旋转
        RotateTwoFace(face1, face2, angle, speed);

        // 等待旋转完成
        while (!rotationComplete)
        {
            yield return null;
        }

        // 移除事件监听
        RotateFaceOver.RemoveListener(() => onRotationComplete?.Invoke());
    }

    public IEnumerator RotateTwoFaceCoroutine(Face face1, Face face2, float angle, float speed)
    {
        bool rotationComplete = false;

        System.Action onRotationComplete = () =>
        {
            rotationComplete = true;
        };

        RotateFaceOver.AddListener(() => onRotationComplete?.Invoke());

        RotateTwoFace(face1, face2, angle, speed);

        while (!rotationComplete)
        {
            yield return null;
        }

        RotateFaceOver.RemoveListener(() => onRotationComplete?.Invoke());
    }

    #endregion

    #region 面层动画方法 - 支持并行（改进版）
    // 改进版面层动画：使用Vector3作为方向系数
    public void MoveFaceAlongNormal(float duration, Face face, Vector3 directionCoefficient)
    {
        MoveFaceAlongNormal(duration, face, directionCoefficient, rotationCurve, 0f);
    }

    public void MoveFaceAlongNormal(float duration, Face face, Vector3 directionCoefficient, AnimationCurve curve)
    {
        MoveFaceAlongNormal(duration, face, directionCoefficient, curve, 0f);
    }

    public void MoveFaceAlongNormal(float duration, Face face, Vector3 directionCoefficient, float delay)
    {
        MoveFaceAlongNormal(duration, face, directionCoefficient, rotationCurve, delay);
    }

    public void MoveFaceAlongNormal(float duration, Face face, Vector3 directionCoefficient, EasingType easingType, float delay = 0f)
    {
        AnimationCurve curve = GetEasingCurve(easingType);
        MoveFaceAlongNormal(duration, face, directionCoefficient, curve, delay);
    }
    public void MoveFaceAlongNormal(float duration, Face face, Vector3 directionCoefficient, AnimationCurve curve, float delay)
    {
        List<GameObject> facePieces = FindFace(face);
        if (facePieces.Count != 4) return;

        // 计算每个角块的移动方向（基于原始局部坐标）
        Dictionary<GameObject, Vector3> directions = new Dictionary<GameObject, Vector3>();
        foreach (GameObject piece in facePieces)
        {
            Vector3 localPos = piece.transform.localPosition;
            Vector3 direction = localPos.normalized;
            direction.Scale(directionCoefficient); // 应用系数
            directions[piece] = direction;
        }

        StartCoroutine(PerformFaceNormalMovement(facePieces, directions, duration, curve, delay));
    }

    // 执行面层动画的协程（独立并行版本）
    private IEnumerator PerformFaceNormalMovement(List<GameObject> pieces,
                                                 Dictionary<GameObject, Vector3> directions,
                                                 float duration, AnimationCurve curve, float delay = 0f)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        // 保存原始位置（在默认父级下）
        Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
        foreach (GameObject piece in pieces)
        {
            originalPositions[piece] = piece.transform.localPosition;
        }

        float elapsedTime = 0f;

        // 向前移动阶段
        while (elapsedTime < duration / 2)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / (duration / 2);
            float easedProgress = curve.Evaluate(progress);

            foreach (GameObject piece in pieces)
            {
                Vector3 startPos = originalPositions[piece];
                Vector3 targetOffset = directions[piece];
                piece.transform.localPosition = Vector3.Lerp(startPos, startPos + targetOffset, easedProgress);
            }

            yield return null;
        }

        // 确保到达目标位置
        foreach (GameObject piece in pieces)
        {
            Vector3 targetOffset = directions[piece];
            piece.transform.localPosition = originalPositions[piece] + targetOffset;
        }

        // 等待短暂停留
        yield return new WaitForSeconds(0.1f);

        elapsedTime = 0f;

        // 返回阶段
        while (elapsedTime < duration / 2)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / (duration / 2);
            float easedProgress = curve.Evaluate(progress);

            foreach (GameObject piece in pieces)
            {
                Vector3 startPos = originalPositions[piece] + directions[piece];
                piece.transform.localPosition = Vector3.Lerp(startPos, originalPositions[piece], easedProgress);
            }

            yield return null;
        }

        // 确保返回原始位置
        foreach (GameObject piece in pieces)
        {
            piece.transform.localPosition = originalPositions[piece];
        }
    }
    #endregion
    void FixedUpdate()
    {
        if (!enableRandomDisplacement || isRotating) return;

        displacementTimer += Time.fixedDeltaTime;

        // 检查是否需要生成新的目标位移
        if (displacementTimer >= displacementUpdateInterval)
        {
            displacementTimer = 0f;

            // 保存当前位移作为新的起始点
            for (int i = 0; i < 8; i++)
            {
                startDisplacements[i] = currentDisplacements[i];
            }

            // 为每个角块生成新的随机目标位移
            for (int i = 0; i < 8; i++)
            {
                if (cornerPieces[i] == null) continue;

                // 生成随机方向的小位移
                Vector3 randomDirection = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                ).normalized;

                targetDisplacements[i] = randomDirection * displacementMagnitude;
            }
        }

        // 计算过渡进度（基于时间）
        float transitionProgress = displacementTimer / displacementUpdateInterval;

        // 应用缓动曲线使过渡更平滑
        float easedProgress = Mathf.SmoothStep(0f, 1f, transitionProgress);

        // 平滑过渡到目标位移
        for (int i = 0; i < 8; i++)
        {
            if (cornerPieces[i] == null) continue;

            // 保存原始位置（如果没有保存过）
            if (!originalDisplacementPositions.ContainsKey(cornerPieces[i]))
            {
                originalDisplacementPositions[cornerPieces[i]] = cornerPieces[i].transform.localPosition;
            }

            // 计算当前帧的目标位移（从startDisplacements过渡到targetDisplacements）
            Vector3 targetDisplacement = Vector3.Lerp(
                startDisplacements[i],
                targetDisplacements[i],
                easedProgress
            );

            // 更新当前位移
            currentDisplacements[i] = targetDisplacement;

            // 应用位移到角块
            Vector3 targetPosition = originalDisplacementPositions[cornerPieces[i]] + targetDisplacement;

            // 平滑移动到目标位置
            cornerPieces[i].transform.localPosition = Vector3.Lerp(
                cornerPieces[i].transform.localPosition,
                targetPosition,
                Time.fixedDeltaTime * displacementSmoothness
            );
        }
    }

    // 重置位移效果
    public void ResetDisplacements()
    {
        for (int i = 0; i < 8; i++)
        {
            if (cornerPieces[i] == null || !originalDisplacementPositions.ContainsKey(cornerPieces[i]))
                continue;

            cornerPieces[i].transform.localPosition = originalDisplacementPositions[cornerPieces[i]];
            currentDisplacements[i] = Vector3.zero;
            startDisplacements[i] = Vector3.zero;
            targetDisplacements[i] = Vector3.zero;
        }

        // 立即生成新的目标位移，以便下一帧开始平滑过渡
        displacementTimer = displacementUpdateInterval; // 这会触发下一次更新
    }

    // 更新位移原始位置（在魔方重置或初始化后调用）
    private void UpdateDisplacementBasePositions()
    {
        originalDisplacementPositions.Clear();
        for (int i = 0; i < 8; i++)
        {
            if (cornerPieces[i] == null) continue;
            originalDisplacementPositions[cornerPieces[i]] = cornerPositions[i];
        }
    }

    // 立即生成新的随机位移（可用于外部调用）
    public void RandomizeDisplacements()
    {
        displacementTimer = displacementUpdateInterval; // 这会触发下一次更新
    }

    // 设置位移参数
    public void SetDisplacementParameters(float magnitude, float interval, float smoothness)
    {
        displacementMagnitude = magnitude;
        displacementUpdateInterval = interval;
        displacementSmoothness = smoothness;
    }
    #region 辅助方法（保持不变）
    void InitializeCube()
    {
        cornerPieces = new GameObject[] { FLU, FRU, FLD, FRD, BLU, BRU, BLD, BRD };
        if (cornerPieces.Length != 8)
        {
            Debug.LogError("需要8个角块！当前数量: " + cornerPieces.Length);
            return;
        }

        cornerPositions[0] = new Vector3(-cubeHalfSize, cubeHalfSize, cubeHalfSize);
        cornerPositions[1] = new Vector3(cubeHalfSize, cubeHalfSize, cubeHalfSize);
        cornerPositions[2] = new Vector3(-cubeHalfSize, -cubeHalfSize, cubeHalfSize);
        cornerPositions[3] = new Vector3(cubeHalfSize, -cubeHalfSize, cubeHalfSize);
        cornerPositions[4] = new Vector3(-cubeHalfSize, cubeHalfSize, -cubeHalfSize);
        cornerPositions[5] = new Vector3(cubeHalfSize, cubeHalfSize, -cubeHalfSize);
        cornerPositions[6] = new Vector3(-cubeHalfSize, -cubeHalfSize, -cubeHalfSize);
        cornerPositions[7] = new Vector3(cubeHalfSize, -cubeHalfSize, -cubeHalfSize);

        for (int i = 0; i < 8; i++)
        {
            if (cornerPieces[i] != null)
            {
                cornerPieces[i].transform.localPosition = cornerPositions[i];
                cornerPieces[i].transform.SetParent(defaultParent);
            }
        }
        // 初始化位移效果的原始位置
        UpdateDisplacementBasePositions();
    }

    public void ResetCube()
    {
        StopAllCoroutines();
        isRotating = false;

        for (int i = 0; i < 8; i++)
        {
            if (cornerPieces[i] != null)
            {
                cornerPieces[i].transform.localPosition = cornerPositions[i];
                cornerPieces[i].transform.localRotation = Quaternion.identity;
                cornerPieces[i].transform.SetParent(defaultParent);
            }
        }

        UpdateFaceLists();

        if (rotationParent != null)
        {
            rotationParent.localRotation = Quaternion.identity;  // 修改：重置局部旋转
        }
        // 重置位移效果
        ResetDisplacements();
        UpdateDisplacementBasePositions();
    }
    public AnimationCurve GetEasingCurve(EasingType easingType)
    {
        switch (easingType)
        {
            case EasingType.Linear:
                return AnimationCurve.Linear(0, 0, 1, 1);

            case EasingType.EaseIn:
                return new AnimationCurve(
                    new Keyframe(0, 0, 0, 0),
                    new Keyframe(1, 1, 2, 0)
                );

            case EasingType.EaseOut:
                return new AnimationCurve(
                    new Keyframe(0, 0, 0, 2),
                    new Keyframe(1, 1, 0, 0)
                );

            case EasingType.EaseInOut:
                return AnimationCurve.EaseInOut(0, 0, 1, 1);

            case EasingType.Bounce:
                return new AnimationCurve(
                    new Keyframe(0, 0),
                    new Keyframe(0.6f, 1.1f),
                    new Keyframe(0.8f, 0.9f),
                    new Keyframe(1, 1)
                );

            default:
                return AnimationCurve.EaseInOut(0, 0, 1, 1);
        }
    }
    public enum Face
    {
        Top,
        Bottom,
        Left,
        Right,
        Front,
        Back
    }

    public enum RotationDirection
    {
        Clockwise,
        CounterClockwise
    }
    public enum EasingType
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
        Bounce
    }
    public List<GameObject> FindFace(Face face)
    {
        switch (face)
        {
            case Face.Top: return new List<GameObject> { FLU, FRU, BLU, BRU };
            case Face.Bottom: return new List<GameObject> { FLD, FRD, BLD, BRD };
            case Face.Left: return new List<GameObject> { FLU, FLD, BLU, BLD };
            case Face.Right: return new List<GameObject> { FRU, FRD, BRU, BRD };
            case Face.Front: return new List<GameObject> { FLU, FRU, FLD, FRD };
            case Face.Back: return new List<GameObject> { BLU, BRU, BLD, BRD };
            default: return new List<GameObject>();
        }
    }

    Vector3 GetRotationAxis(Face face)
    {
        Vector3 localAxis;
        switch (face)
        {
            case Face.Top: localAxis = Vector3.up; break;
            case Face.Bottom: localAxis = Vector3.down; break;
            case Face.Left: localAxis = Vector3.left; break;
            case Face.Right: localAxis = Vector3.right; break;
            case Face.Front: localAxis = Vector3.forward; break;
            case Face.Back: localAxis = Vector3.back; break;
            default: localAxis = Vector3.up; break;
        }
        return localAxis;  // 修改：直接返回局部轴，不需要转换
    }

    public void UpdateFaceLists()
    {
        foreach (GameObject piece in cornerPieces)
        {
            if (piece == null) continue;

            Vector3 p = piece.transform.localPosition;
            float x = p.x, y = p.y, z = p.z;

            if (y > 0 && x < 0 && z > 0) FLU = piece;
            else if (y > 0 && x > 0 && z > 0) FRU = piece;
            else if (y < 0 && x < 0 && z > 0) FLD = piece;
            else if (y < 0 && x > 0 && z > 0) FRD = piece;
            else if (y > 0 && x < 0 && z < 0) BLU = piece;
            else if (y > 0 && x > 0 && z < 0) BRU = piece;
            else if (y < 0 && x < 0 && z < 0) BLD = piece;
            else if (y < 0 && x > 0 && z < 0) BRD = piece;
        }
        cornerPieces = new GameObject[] { FLU, FRU, FLD, FRD, BLU, BRU, BLD, BRD };
        // 更新位移效果的原始位置
        UpdateDisplacementBasePositions();
    }
    #endregion
}