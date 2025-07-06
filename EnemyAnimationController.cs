using UnityEngine;
using System.Collections;

public class EnemyAnimationController : MonoBehaviour
{
    [SerializeField] private AnimationInstancing _animInstancing;
    [SerializeField] private float _crossfadeDuration = 0.3f;

    private Coroutine _revertCoroutine;

    public void PlayIdle()
    {
        PlayAnimation(0);
    }

    public void PlayWalk()
    {
        PlayAnimation(1);
    }

    public void PlayAttackAnimation()
    {
        PlayAnimationThenRevert(2, 1.0f);
    }

    public void PlayHitAnimation()
    {
        if (!gameObject.activeInHierarchy) return;
        PlayAnimationThenRevert(1, 0.3f);
    }

    public void PlayDeathAnimation()
    {
        PlayAnimation(4);
    }

    private void PlayAnimation(int index)
    {
        if (_revertCoroutine != null)
        {
            StopCoroutine(_revertCoroutine);
            _revertCoroutine = null;
        }

        _animInstancing.CrossFade(index, _crossfadeDuration);
    }

    private void PlayAnimationThenRevert(int index, float revertDelay)
    {
        if (!gameObject.activeInHierarchy) return;
        if (_revertCoroutine != null)
        {
            StopCoroutine(_revertCoroutine);
        }

        _animInstancing.CrossFade(index, _crossfadeDuration);
        _revertCoroutine = StartCoroutine(RevertToIdle(revertDelay));
    }

    private IEnumerator RevertToIdle(float delay)
    {
        yield return new WaitForSeconds(delay);
        _animInstancing.CrossFade(0, _crossfadeDuration);
        _revertCoroutine = null;
    }
}
