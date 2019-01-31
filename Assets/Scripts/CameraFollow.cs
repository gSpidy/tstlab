using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class CameraFollow : MonoBehaviourSingleton<CameraFollow> {

	public Transform target;
	[SerializeField] private float m_distance = 10;
	
	void Start ()
	{
		//init camera movement
		Observable.EveryUpdate()
			.Where(_ => target)
			.Select(_ => target.position + Vector3.back * m_distance)
			.Scan(target.position, (pos, tpos) => Vector3.Lerp(pos, tpos, Time.deltaTime))
			.Select(FitInLevel) //clamp in level rect
			.Subscribe(x => transform.position = x)
			.AddTo(this);
	}

	private Rect? _cpr;
	private Rect CameraPosRect
	{
		get
		{
			if (_cpr.HasValue) return _cpr.Value;
			
			var downLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, m_distance));
			var upRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, m_distance));

			Vector2 camWorldHalfSz = (upRight - downLeft)/2;

			var min = Level.Instance.WorldMinimum + camWorldHalfSz;
			var max = Level.Instance.WorldMaximum - camWorldHalfSz;

			return (_cpr=new Rect(min, max - min)).Value;
		}
	}

	Vector3 FitInLevel(Vector3 sourcePos) => new Vector3(
		Mathf.Clamp(sourcePos.x,CameraPosRect.xMin,CameraPosRect.xMax),
		Mathf.Clamp(sourcePos.y,CameraPosRect.yMin,CameraPosRect.yMax),
		sourcePos.z
	);
}
