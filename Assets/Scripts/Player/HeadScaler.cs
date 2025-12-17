using UnityEngine;

public class HeadScaler : MonoBehaviour
{
	[Header("Head Scaling")]
	[SerializeField] private float headScale = 0.1f;
	[SerializeField] private Vector3 headOffset = Vector3.zero;
	[SerializeField] private Transform headBone;
	[SerializeField] private bool showDebug = false;

	private Vector3 _originalLocalScale;
	private Vector3 _originalLocalPosition;

	private void Awake()
	{
		if (headBone == null)
		{
			var animator = GetComponentInParent<Animator>();
			if (animator != null && animator.isHuman)
			{
				headBone = animator.GetBoneTransform(HumanBodyBones.Head);
			}
		}

		if (headBone != null)
		{
			_originalLocalScale = headBone.localScale;
			_originalLocalPosition = headBone.localPosition;
		}
		else if (showDebug)
		{
			Debug.LogWarning($"[{nameof(HeadScaler)}] No head bone assigned/found on {name}.");
		}
	}

	private void LateUpdate()
	{
		if (headBone == null)
			return;

		// Apply as a multiplier around 1.0 for safer tuning.
		float scaleMultiplier = Mathf.Max(0.01f, 1f + headScale);
		headBone.localScale = _originalLocalScale * scaleMultiplier;
		headBone.localPosition = _originalLocalPosition + headOffset;
	}
}

