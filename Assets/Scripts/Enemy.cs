using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

public class Enemy : BaseCharacter
{
	private ReactiveProperty<BaseCharacter> Target = new ReactiveProperty<BaseCharacter>(null);

	[SerializeField] private int m_baseFindDist = 4, m_baseLostDist = 6;
	private int _findAdder=0,_lostAdder=0;

	private int FindDist => m_baseFindDist + _findAdder;
	private int LostDist => m_baseLostDist + _lostAdder;
	
	public override void ApplyLevelSettings(int lvl)
	{
		switch (lvl / 20)
		{
			case 1:
				_findAdder = 1;
				_lostAdder = 2;
				break;
			case 2:
				_findAdder = 1;
				_lostAdder = 3;
				break;
			default: 
				break;
		}
	}

	private List<BaseCharacter> _others;
	public List<BaseCharacter> Others => _others??(_others = FindObjectsOfType<BaseCharacter>()
	                                     .Where(x=>!(x is Enemy))
	                                     .ToList());

	Vector2 Wander() => Random.insideUnitCircle;

	protected override void InitializeLogic()
	{
		//chase target or wander
		Target
			.Select(tgt =>
			{
				return Observable.ReturnUnit()
					.Select(_ => tgt?
						Level.Instance.PathStepDir(LevelPosition.Value, tgt.LevelPosition.Value)
						:Wander())
					.SelectMany(dir => Move(Vector2.Reflect(dir,Vector2.up)))
					.Repeat();
			})
			.Switch()
			.Subscribe()
			.AddTo(this);
		
		//find/lost target
		Target
			.Select(tgt =>
			{
				if (!tgt)
				{
					return Observable.IntervalFrame(10)
						.Select(_ =>
							Others.FirstOrDefault(x => (x.LevelPosition.Value - LevelPosition.Value).magnitude <= FindDist))
						.Where(x => x);
				}
				else
				{
					return Observable.IntervalFrame(10)
						.Where(_ => (tgt.LevelPosition.Value - LevelPosition.Value).magnitude >= LostDist)
						.Select(_ => null as BaseCharacter);
				}
			})
			.Switch()
			.Subscribe(tgt =>
			{
				Target.Value = tgt;
				Debug.Log(name + " " + (tgt?$"targeted {tgt.name}":"lost target"));
			})
			.AddTo(this);
	}
}
