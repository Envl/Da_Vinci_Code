using UnityEngine;
using System.Collections;

public class ArrowForJoker : MonoBehaviour {
	public Transform ParentBrick;//是谁生成的这个arrow  

	//目标位置左边那个Brick的引用. 通过这个就有办法定位到
	//要把parentBrick放置到Decks[myDeck]这个List中的哪个位置去
	public Transform LNeiborOfDest;
	//就是目标位置在当前位置的左边
	public bool DestAtLeft=false;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnMouseDown(){
		print("Click Arrow");
		//先从列表移除
		GameControl.Instance.Decks[0].Remove(ParentBrick);
		int offIndex=1;
		if(DestAtLeft){
			offIndex=0;
		}
		int myIndex=GameControl.Instance.Decks[0].IndexOf(LNeiborOfDest);
		//再加入到目标位置  利用左邻居得到了自己的index
		GameControl.Instance.Decks[0].Insert(myIndex+offIndex,ParentBrick);
		//通知其他玩家我pick了这张牌到哪里
		GameControl.Instance
			.Client.OpRaiseEvent(MyEventCode.OtherPickABrick,
				new int[]{GameControl.Instance.MyID,
					GameControl.Instance.AllBricks.IndexOf(ParentBrick),
					myIndex+offIndex},
					true,
				ExitGames.Client.Photon.LoadBalancing.RaiseEventOptions.Default);

		//对自己的deck进行视觉更新
		GameControl.Instance.PlaceBricks(0);
		//完成任务后销毁所有arrow
		ParentBrick.GetComponent<Brick>().DestroyArrows();
	}
}
