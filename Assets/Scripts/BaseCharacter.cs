using System;
using DG.Tweening;
using UniRx;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public abstract class BaseCharacter : MonoBehaviour
{
	[SerializeField] protected float m_moveSpeed = 1;
	
	[SerializeField] private Sprite m_frame1, m_frame2;

	private SpriteRenderer _sr;
	private SpriteRenderer Renderer => _sr ?? (_sr = GetComponent<SpriteRenderer>());
	
	public float TimeToCrossCell => 1f / m_moveSpeed;
	public ReactiveProperty<Vector2Int> LevelPosition { get; } = new ReactiveProperty<Vector2Int>(Vector2Int.zero);

	protected Vector2Int NormalizeDirection(Vector2 direction)
	{
		var x = Vector3.Project(direction, Vector3.right);
		var y = Vector3.Project(direction, Vector3.up);

		Vector2 newDir = x.sqrMagnitude > y.sqrMagnitude ? x.normalized : y.normalized;
		
		return Vector2Int.FloorToInt(newDir);
	}
	
	protected IObservable<Unit> Move(Vector2 direction)
	{
		if (direction.magnitude < .1f) return Observable.NextFrame();
		
		var intDir = Vector2Int.FloorToInt(NormalizeDirection(direction));
		
		var dest = LevelPosition.Value + intDir;
		if (Level.Instance[dest.x, dest.y]) return Observable.NextFrame();
		
		return Observable.ReturnUnit()
			.Do(_ =>
			{
				transform.DOMove((Vector2)(LevelPosition.Value+intDir), TimeToCrossCell);
				
				var quarterTime = TimeToCrossCell / 4;

				DOTween.Sequence()
					.AppendInterval(quarterTime)
					.AppendCallback(() =>
					{
						if(Renderer) Renderer.sprite = m_frame2;
					})
					.AppendInterval(quarterTime)
					.AppendCallback(() =>
					{
						if(Renderer) Renderer.sprite = m_frame1;
					})
					.SetLoops(2);
			})
			.Delay(TimeSpan.FromSeconds(TimeToCrossCell))
			.Do(_ => LevelPosition.Value = LevelPosition.Value + intDir);
	}

	protected void Start()
	{
		SyncLevelPosition();
		InitializeLogic();
	}

	public void SyncLevelPosition()
	{
		LevelPosition.Value = Vector2Int.FloorToInt(transform.position);
	}

	public virtual void ApplyLevelSettings(int lvl){}
	protected abstract void InitializeLogic();

}
