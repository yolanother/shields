using System.Collections;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Shield : MonoBehaviour
{
    Renderer _renderer;
    [SerializeField] AnimationCurve _DisplacementCurve;
    [SerializeField] float _DisplacementMagnitude;
    [SerializeField] float _LerpSpeed;
    [SerializeField] float _DisolveSpeed;
    [SerializeField] private float _ImpactDuration = 2;

    [SerializeField] private UnityEvent<Collision> onShieldImpact = new UnityEvent<Collision>();
    [SerializeField] public UnityEvent onShieldActive = new UnityEvent();
    [SerializeField] public UnityEvent onShieldInactive = new UnityEvent();

    bool _shieldOn;
    Coroutine _disolveCoroutine;
    // Start is called before the first frame update
    void Start()
    {
        _renderer = GetComponent<Renderer>();
    }

    private void OnCollisionEnter(Collision other)
    {
        onShieldImpact?.Invoke(other);
        HitShield(other.GetContact(0).point);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                HitShield(hit.point);
            }
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            OpenCloseShield();
        }
    }

    public void HitShield(Vector3 hitPos)
    {
        _renderer.material.SetVector("_HitPos", hitPos);
        StopAllCoroutines();
        StartCoroutine(Coroutine_HitDisplacement());
    }

    public void OpenCloseShield()
    {
        float target = 1;
        if (_shieldOn)
        {
            target = 0;
        }
        _shieldOn = !_shieldOn;
        SetDissolve(target);
    }

    IEnumerator Coroutine_HitDisplacement()
    {
        float lerp = 0;
        while (lerp < 1)
        {
            _renderer.material.SetFloat("_DisplacementStrength", _DisplacementCurve.Evaluate(lerp) * _DisplacementMagnitude);
            lerp += Time.deltaTime*_LerpSpeed;
            yield return null;
        }

        yield return new WaitForSeconds(_ImpactDuration);

        while (lerp > 0)
        {
            _renderer.material.SetFloat("_DisplacementStrength",
                _DisplacementCurve.Evaluate(lerp) * _DisplacementMagnitude);
            lerp -= Time.deltaTime * _LerpSpeed;
            yield return null;
        }
    }

    IEnumerator Coroutine_DisolveShield(float target)
    {
        float start = _renderer.material.GetFloat("_Disolve");
        float lerp = 0;
        while (lerp < 1)
        {
            _renderer.material.SetFloat("_Disolve", Mathf.Lerp(start,target,lerp));
            lerp += Time.deltaTime * _DisolveSpeed;
            yield return null;
        }

        if (target >= 1)
        {
            onShieldInactive?.Invoke();
        }

        if (target <= 0)
        {
            onShieldActive?.Invoke();
        }

    }

    public void SetDissolve(float dissolve)
    {
        if (_disolveCoroutine != null)
        {
            StopCoroutine(_disolveCoroutine);
        }

        _disolveCoroutine = StartCoroutine(Coroutine_DisolveShield(dissolve));
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Shield))]
public class ShieldEditor : Editor
{
    [SerializeField] private Transform impactObject;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (Application.isPlaying)
        {
            var shield = (Shield) target;
            if (GUILayout.Button("Open/Close Shield"))
            {
                shield.OpenCloseShield();
            }

            impactObject = (Transform) EditorGUILayout.ObjectField("Impact Object", impactObject, typeof(Transform));
            if (GUILayout.Button("Hit"))
            {
                shield.HitShield(impactObject.position);
            }
        }
    }
}

#endif
