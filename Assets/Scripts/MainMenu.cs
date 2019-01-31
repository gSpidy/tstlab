using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
	[SerializeField] private Button m_originalLevelBtn;

	// Use this for initialization
	void Start ()
	{
		m_originalLevelBtn.gameObject.SetActive(false);
		
		Observable.Range(0, GameController.MAX_LEVELS)
			.SelectMany(idx =>
			{
				var btn = m_originalLevelBtn.InstantiateMe(m_originalLevelBtn.transform.parent);
				btn.gameObject.SetActive(true);
				
				btn.GetComponentInChildren<Text>().text = (idx + 1).ToString();

				return btn.OnClickAsObservable().Select(_ => idx);
			})
			.First()
			.Subscribe(StartGame)
			.AddTo(this);
	}

	public void StartGame(int level)
	{
		GameController.LevelNum = level;
		SceneManager.LoadScene("Game");
	}

	public void QuitBtn()
	{
		Application.Quit();
	}
}
