using System;
using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class PopupDialog : MonoBehaviourSingleton<PopupDialog>
{
	[SerializeField] private RectTransform m_body;
	[SerializeField] private Text m_msgText,m_btnText;
	[SerializeField] private Button m_btn;

	private void Awake()
	{
		var _=Instance;
		gameObject.SetActive(false);
	}

	public IObservable<Unit> Show(string message, string button)
	{
		m_msgText.text = message;
		m_btnText.text = button;

		gameObject.SetActive(true);

		GetComponent<CanvasGroup>().DOFade(0, .4f).From();
		m_body.DOScale(Vector3.one * .3f, .4f).From().SetEase(Ease.OutBack);
		
		return m_btn.OnClickAsObservable()
			.DoOnCompleted(()=>gameObject.SetActive(false))
			.Publish()
			.RefCount();
	}
}
