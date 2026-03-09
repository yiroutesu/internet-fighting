using UnityEngine;
using UnityEngine.InputSystem;

public class RealBallFadeAlpha : MonoBehaviour
{
    [SerializeField] GameObject spherePrefab;
    [SerializeField] InputActionReference menuAction;
    [SerializeField] float farRadius  = 20f;
    [SerializeField] float nearRadius = 3f;
    [SerializeField] float duration   = 0.3f;
    [SerializeField] Canvas uiMenuPrefab;   // 拖入菜单
    [SerializeField] float uiDIstance=2.5f;

    Transform sphereTx;
    Renderer  sphereRend;
    Material  sphereMat;
    bool      isInMenu = false;
    float     timer    = 0;

    bool isUiInstance;          // ✅ 保存生成的 UI

    void Start()
    {
        sphereTx    = Instantiate(spherePrefab).transform;
        sphereRend  = sphereTx.GetComponent<Renderer>();
        sphereMat   = sphereRend.material;
        sphereTx.SetParent(Camera.main.transform, false);
        sphereTx.localPosition = Vector3.zero;
        sphereTx.localScale    = Vector3.one * farRadius;
        sphereRend.enabled     = false;
        uiMenuPrefab.worldCamera=UnityEngine.Camera.main;
        uiMenuPrefab.gameObject.SetActive(false);
        Toggle();
    }

    void OnEnable()
    {
        menuAction.action.performed += _ => Toggle();
        menuAction.action.Enable();
    }
    void OnDisable() => menuAction.action.Disable();

    public void Toggle()
    {
        Debug.Log("leftmenu is pressed");
        isInMenu = !isInMenu;
        timer    = 0;
        sphereRend.enabled = true;

        if (isInMenu)
        {
            // 打开：远->近，透明->不透明
            sphereTx.localScale = Vector3.one * farRadius;
            sphereMat.SetFloat("_Alpha", 0f);

            // 生成 UI
            if (!isUiInstance)
            {
                Vector3 uiWorldPos = Camera.main.transform.position + Camera.main.transform.forward * uiDIstance;
                uiMenuPrefab.gameObject.SetActive(true);
                uiMenuPrefab.transform.position=uiWorldPos;
                uiMenuPrefab.transform.LookAt(UnityEngine.Camera.main.transform);
                isUiInstance=true;
            }
        }
        else
        {
            // 关闭：近->远，不透明->透明
            sphereTx.localScale = Vector3.one * nearRadius;
            sphereMat.SetFloat("_Alpha", 1f);

            //  销毁 UI
            if (isUiInstance)
            {
                uiMenuPrefab.gameObject.SetActive(false);
                isUiInstance=false;
            }
        }
    }

    void Update()
    {
        if (!sphereRend.enabled) return;

        timer = Mathf.MoveTowards(timer, 1, Time.deltaTime / duration);
        float r     = isInMenu ? Mathf.Lerp(farRadius, nearRadius, timer)
                               : Mathf.Lerp(nearRadius, farRadius, timer);
        float alpha = isInMenu ? Mathf.Lerp(0, 1, timer)
                               : Mathf.Lerp(0, 1, 1 - timer);

        sphereTx.localScale = Vector3.one * r;
        sphereMat.SetFloat("_Alpha", alpha);

        if (alpha <= 0.01f && r >= farRadius - 0.01f)
            sphereRend.enabled = false;
    }
}