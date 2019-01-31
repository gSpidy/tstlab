using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
	public static BehaviorSubject<Vector2> MoveDirection { get; } = new BehaviorSubject<Vector2>(Vector2.zero);
	public static BehaviorSubject<float> Progress { get; } = new BehaviorSubject<float>(0);
	public static IntReactiveProperty DisplayLevel { get; } = new IntReactiveProperty(0);

	[SerializeField] private Image m_joy, m_joyHandle;
	[SerializeField] private Slider m_coinsSlider;
	[SerializeField] private Text m_levelText,m_finalText;
	
	void Start ()
	{
		//animated coins count display
		Progress
			.Select(Mathf.Clamp01)
			.Subscribe(x => m_coinsSlider.DOValue(x, .5f))
			.AddTo(this);

		DisplayLevel
			.SubscribeToText(m_levelText, x => $"Level {x + 1}")
			.AddTo(this);

		Progress
			.Select(x => x >= 1)
			.Subscribe(show =>
			{
				m_finalText.gameObject.SetActive(show);

				var originalColor = m_finalText.color;
				if (show)
					m_finalText.DOFade(0, .4f).SetLoops(-1, LoopType.Yoyo);
				else
				{
					m_finalText.DOKill();
					m_finalText.color = originalColor;
				}
			})
			.AddTo(this);

		InitJoystick();
	}

	private void InitJoystick()
	{
		var joySz = m_joy.rectTransform.sizeDelta / 2 - m_joyHandle.rectTransform.sizeDelta / 2;
		
		var touchInputObs = m_joy.OnPointerDownAsObservable()
			.SelectMany(pd =>
			{
				return Observable.EveryUpdate()
					.Select(_ => (Vector2) m_joy.transform.InverseTransformPoint(pd.position))
					.Select(pos=>new Vector2(pos.x/joySz.x,pos.y/joySz.y))
					.Select(x=>Vector2.ClampMagnitude(x,1))
					.DistinctUntilChanged()
					.TakeUntil(m_joy.OnPointerUpAsObservable().Where(pd1 => pd1.pointerId == pd.pointerId))
					.Concat(Observable.Return(Vector2.zero));
			})
			.Publish()
			.RefCount();

		//joystick emulation with keyz 4 editor
#if UNITY_EDITOR
		var updObs = Observable.EveryUpdate();
		var keysObs = Observable.Merge(new[]
			{
				updObs.Where(_ => Input.GetKey(KeyCode.UpArrow)).Select(_ => new Vector2(0, 1)),
				updObs.Where(_ => Input.GetKey(KeyCode.DownArrow)).Select(_ => new Vector2(0, -1)),
				updObs.Where(_ => Input.GetKey(KeyCode.LeftArrow)).Select(_ => new Vector2(-1, 0)),
				updObs.Where(_ => Input.GetKey(KeyCode.RightArrow)).Select(_ => new Vector2(1, 0))
			})
			.Publish()
			.RefCount();
#endif

		var inputObs = touchInputObs
#if UNITY_EDITOR
			.Merge(keysObs, keysObs.ThrottleFrame(3).Select(_ => Vector2.zero))
#endif
			.DistinctUntilChanged()
			.Publish()
			.RefCount();

		//inputObs.Subscribe(x => Debug.Log(x)).AddTo(this);

		inputObs
			.Select(x => new Vector2(x.x * joySz.x, x.y * joySz.y))
			.Subscribe(x => m_joyHandle.transform.localPosition = x)
			.AddTo(this);

		inputObs
			.Subscribe(MoveDirection)
			.AddTo(this);
	}

	public void ToMenuBtn()
	{
		SceneManager.LoadScene("Main");
	}
	
}
