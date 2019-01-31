using UniRx;
using UnityEngine;

public class Player : BaseCharacter
{
	protected override void InitializeLogic()
	{
		GameUI.MoveDirection
			.First(x => x != Vector2.zero)
			.SelectMany(dir=>
			{
				transform.up = (Vector2)NormalizeDirection(dir);
				return Move(dir);
			})
			.Repeat()
			.Subscribe()
			.AddTo(this);
	}
}
