using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour
{
	public const int MAX_LEVELS = 50;
	
	public static int LevelNum = 0;

	[SerializeField] private BaseCharacter m_playerPrefab;
	[SerializeField] private BaseCharacter m_botPrefab;
	[SerializeField] private GameObject m_collectablePrefab;
	
	private void Awake()
	{
		SceneManager.LoadScene("GameUI",LoadSceneMode.Additive);

		GameUI.DisplayLevel.Value = LevelNum;
		
		Level.Instance.Generate(LevelNum,true);
		Level.Instance.RenderField();

		// ReSharper disable once PossibleLossOfFraction
		var player = m_playerPrefab.InstantiateMe(Level.FIELD_SIZE / 2 * Vector2.one, Quaternion.identity);
		CameraFollow.Instance.target = player.transform;

		var progressObs = CollectProgressObs(SpawnCollectables(Level.CellsEnumerable),
			player.LevelPosition
				.Select(p=>new Vector2Int(p.x,Level.FIELD_SIZE-1-p.y)));

		progressObs
			.StartWith(0)
			.Subscribe(x=>GameUI.Progress.OnNext(x)) //subscribe progress ui to this observable
			.AddTo(this);
		
		//win condition
		var winc = progressObs
			.AsUnitObservable()
			.Concat(player.LevelPosition.Where(x=>x==Level.Instance.ExitPosition).Take(1).AsUnitObservable()) //go to exit when collected all
			.Last();

		//lose condition
		var losec = SpawnBots(Level.CellsEnumerable)
			.Select(x => x.LevelPosition) //observe all enemies positions
			.Merge()
			.CombineLatest(player.LevelPosition.Skip(1), (enpos, plpos) =>
			{
				var dir = plpos-enpos;
				return dir.sqrMagnitude <= 1 && Level.Instance.CanStep(enpos, dir);
			})
			.First(x=>x);
		
		//finish game once
		new []
		{
			winc.Select(_=>true),
			losec.Select(_=>false)
		}
			.Merge()
			.First()
			.Subscribe(isWin=>
			{
				Destroy(player.gameObject);
				
				if (isWin)
					Win();
				else
					Lose();
			})
			.AddTo(this);
	}

	void Win()
	{
		Debug.Log("WIN!");

		PopupDialog.Instance.Show("WIN", "Next level")
			.Subscribe(_ =>
			{
				if (LevelNum < MAX_LEVELS - 1) LevelNum++;
				SceneManager.LoadScene("Game");
			})
			.AddTo(this);
	}

	void Lose()
	{
		PopupDialog.Instance.Show("You lose", "Retry")
			.Subscribe(_ => SceneManager.LoadScene("Game"))
			.AddTo(this);	
	}

	List<BaseCharacter> SpawnBots(IEnumerable<Vector2Int> variants)
	{
		var num = LevelNum / 10 + 1; //+1 bot every 10th lvl

		return variants
			.OrderBy(_ => Random.value)
			.Take(num)
			.Select(x => m_botPrefab.InstantiateMe((Vector2) x, Quaternion.identity))
			.Do(x=>x.ApplyLevelSettings(LevelNum))
			.ToList();
	}

	List<GameObject> SpawnCollectables(IEnumerable<Vector2Int> variants)
	{
		var num = LevelNum / 3 + 1; //+1 coin every 3rd lvl
		
		var occupied = new List<Vector2Int>();

		return variants
			.OrderBy(_ => Random.value)
			.Where(pending => occupied.All(occ => Level.Instance.PathLength(pending, occ) > 5))
			.Take(num)
			.Select(x =>
			{
				occupied.Add(x);
				return m_collectablePrefab.InstantiateMe(new Vector2(x.x,Level.FIELD_SIZE-1-x.y), Quaternion.identity);
			})
			.ToList();
	}

	IObservable<float> CollectProgressObs(List<GameObject> collectables, IObservable<Vector2Int> playerPos) => playerPos
		.Select(pos => Physics2D.OverlapPoint(pos))
		.Where(c => c && collectables.Contains(c.gameObject))
		.Do(c => Destroy(c.gameObject)) //destroy collectable
		.Scan(0, (cnum, _) => ++cnum)
		.Take(collectables.Count)
		.Select(x => (float) x / collectables.Count)
		.Publish()
		.RefCount();
}