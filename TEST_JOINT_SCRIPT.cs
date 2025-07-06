using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;


public class TEST_JOINT_SCRIPT : MonoBehaviour
{
    [SerializeField] private Ease _easeType;
    [SerializeField, Range(0.0f,1f)] private float _duration;
    private Vector3 _startRotation;

    private void Awake()
    {
        _startRotation = transform.rotation.eulerAngles;
    }

    private void Update() 
    {
        
    }


    [Button("TO FLOOR")]
    public void ReturnStartRotation()
    {
        transform.rotation = Quaternion.Euler(_startRotation);
    }

    [Button("STAND")]
    public void TestMethod()
    {
        Debug.Log("Test Method");

        Tween tween = transform.DORotateQuaternion(Quaternion.Euler(0, 0, 0), _duration);

        tween.SetEase(_easeType);
        tween.OnComplete(() =>
        {
            Debug.Log("Rotation Completed");
        });
        tween.Play();
    }
}
