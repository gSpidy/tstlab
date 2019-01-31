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

	protected Vector2 NormalizeDirection(Vector2 direction)
	{
		var x = Vector3.Project(direction, Vector3.right);
		var y = Vector3.Project(direction, Vector3.up);

		return x.sqrMagnitude > y.sqrMagnitude ? x.normalized : y.normalized;
	}
	
	protected IObservable<Unit> Move(Vector2 direction)
	{
		if (direction.magnitude < .1f) return Observable.NextFrame();

		var normalDir = NormalizeDirection(direction);
		var intDir = Vector2Int.FloorToInt(Vector2.Reflect(normalDir,Vector2.up));

		var intDest = LevelPosition.Value + intDir;
		
		if(!Level.Instance.CanStep(LevelPosition.Value,intDir)) return Observable.NextFrame();
		
		return Observable.ReturnUnit()
			.Do(_ =>
			{
				transform.DOMove(new Vector2(intDest.x,Level.FIELD_SIZE - 1 - intDest.y), TimeToCrossCell);
				
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
		LevelPosition.Value = Vector2Int.FloorToInt(new Vector2(transform.position.x,Level.FIELD_SIZE-1-transform.position.y));
	}

	public virtual void ApplyLevelSettings(int lvl){}
	protected abstract void InitializeLogic();

}
