using UnityEngine;
using System.Collections;

public class PlayerIcon : MonoBehaviour {
	public int playerID;
	public int range=20;
	public int slowness=50;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		//本id的玩家 活动中  头像动画
		if(GameControl.Instance.CurrentID==playerID){
			transform.rotation=Quaternion.Euler(0,0,
				Mathf.Sin(2*Mathf.PI*(Time.frameCount%slowness)/slowness)*range);
			
		}
	}

	void OnMouseDown(){
		
	}
}
