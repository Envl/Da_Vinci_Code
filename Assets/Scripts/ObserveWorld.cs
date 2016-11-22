using UnityEngine;
using System.Collections;

public class ObserveWorld : MonoBehaviour {
	//planet旋转的范围
	int _zMin=-10,_zMax=10,_xMin=-10,_xMax=10,_yMin=-30,_yMax=30;

	Vector3 _lastPos;
	bool _mouseDown=false;
	Transform _planet;
	// Use this for initialization
	void Start () {
		_planet=GameControl.Instance.Planet;
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetMouseButtonDown(0)){
			_lastPos=Input.mousePosition;
			_mouseDown=true;
		}

		if(Input.GetMouseButtonUp(0)){
			_mouseDown=false;
		}

		if(_mouseDown){
			Vector3 newEulerRotation=new Vector3(_planet.localEulerAngles.x
				+(Input.mousePosition.x-_lastPos.x)*20/Screen.width,
				_planet.localEulerAngles.y,
				_planet.localEulerAngles.z+(Input.mousePosition.y-_lastPos.y)*40/Screen.height
				);
				
			_lastPos=Input.mousePosition;
			//print(newEulerRotation);
			if( (newEulerRotation.x<10||newEulerRotation.x>350)
				&&(Mathf.Abs(newEulerRotation.z)<5||newEulerRotation.z>350)
				&&Mathf.Abs(newEulerRotation.y)<30){
					_planet.localEulerAngles=newEulerRotation;
			}
		}

	/*	if(_mouseDown){
			Vector3 newEulerRotation=new Vector3(transform.localEulerAngles.x
				+(Input.mousePosition.x-_lastPos.x)*20/Screen.width,
				transform.localEulerAngles.y,
				transform.localEulerAngles.z+(Input.mousePosition.y-_lastPos.y)*40/Screen.height
			);

			_lastPos=Input.mousePosition;
			//print(newEulerRotation);
			if( (newEulerRotation.x<10||newEulerRotation.x>350)
				&&(Mathf.Abs(newEulerRotation.z)<5||newEulerRotation.z>345)
				&&Mathf.Abs(newEulerRotation.y)<30){
				transform.localEulerAngles=newEulerRotation;
			}
		}*/

	}
}
