using UnityEngine;
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using ExitGames.Client.Photon.LoadBalancing;
using ExitGames.Client.Photon;

public class PhotonClient : LoadBalancingClient {
	public delegate void EventHandler(EventData data);
	public event EventHandler OtherLeave=delegate {};
	public event EventHandler DebugAnyEventDataReceived=delegate {};
	public event EventHandler OtherJoin=delegate {};

	public delegate void SyncBrickEventHandler(int[] positions);
	public event SyncBrickEventHandler SyncAllBricks=delegate {};

	// msgType -1  只有whitejoker   0  只有blackJoker   1 两者都有
	public delegate void SyncJokerEventHandler(int id,int msgType,int whiteDestIndex,int blackDestIndex);
	public event SyncJokerEventHandler SyncJoker=delegate{};

	public delegate void OtherHandleABrickEventHandler(int id,int indexInAllBricks);
	//玩家向房主进行pick的请求    只有房主处理这条消息, 其他人忽略
	public event OtherHandleABrickEventHandler  RequestInitialPick=delegate{};
	//房主广播这条消息  同意某玩家pick他请求的那张牌.  然后除了房主以外的所有玩家响应该消息
	public event OtherHandleABrickEventHandler ConfirmInitialPick=delegate{};

	public delegate void OtherPickABrickEventHandler(int id,int indexInAllBricks,int destIndexInDeck);
	public event OtherPickABrickEventHandler OtherPickABrick=delegate{};

	public delegate void NoParamEventHandler();
	public event NoParamEventHandler JoinedRoom=delegate{};
	public event NoParamEventHandler GO=delegate{};
	public event NoParamEventHandler ConnectedToServer=delegate{};
	public event NoParamEventHandler NoSuchRoom=delegate{};
	public event NoParamEventHandler CreatedRoom=delegate{};
	public event NoParamEventHandler UpdateID=delegate{};

	public delegate void MsgNeedOneInt(int theInt);
	public event MsgNeedOneInt OtherReadyToGo=delegate{};
	public event MsgNeedOneInt OrderOnesBrick=delegate{};// 对某个玩家的牌进行排序 该玩家自己才会发出这条消息
	public event MsgNeedOneInt OtherOpenABrick=delegate{};
	public event MsgNeedOneInt NextOne=delegate{};
	public event MsgNeedOneInt GameOver=delegate{};
	public event MsgNeedOneInt OneDone=delegate {};
	public event MsgNeedOneInt OneLeave=delegate{};
	public event MsgNeedOneInt OnePlayAgain=delegate{};

	public delegate void DebugPrintOperationResponse(OperationResponse resp);
	public event DebugPrintOperationResponse DebugResponse=delegate{};

	int joinedCount=0;//因为joined消息会受到两次... 所以只在第二次进行响应
	public override void OnOperationResponse(OperationResponse operationResponse){
		base.OnOperationResponse(operationResponse);
		DebugResponse(operationResponse);

		switch (operationResponse.OperationCode){
		/*case OperationCode.Join:
			JoinedRoom();
			break;*/
		case OperationCode.CreateGame:
			//成功创建房间
			if (operationResponse.ReturnCode==0){
				CreatedRoom();
			}
			break;
		case OperationCode.JoinGame://这个消息莫名其妙会收到两次
			//成功进入房间
			if (operationResponse.ReturnCode==0){
				joinedCount++;
				if(joinedCount==2){
					JoinedRoom();
					joinedCount=0;
				}
			}
			//房间不存在
			else if(operationResponse.ReturnCode==32758){
				NoSuchRoom();
			}
			break;
		}


	}


	public override void OnEvent(EventData phoEvent){
		base.OnEvent(phoEvent);

		DebugAnyEventDataReceived(phoEvent);
	
		switch(phoEvent.Code){
		case EventCode.Leave:
			OtherLeave(phoEvent);

			break;
		case EventCode.Join:
			OtherJoin(phoEvent)	;		
			break;
		case EventCode.GameList:
			ConnectedToServer();
			break;
		case MyEventCode.SyncAllBricks:
			SyncAllBricks(phoEvent.Parameters[ParameterCode.CustomEventContent]as int[]);
			break;
		case MyEventCode.OtherPickABrick:
			int[] tmp=phoEvent.Parameters[ParameterCode.CustomEventContent]as int[];
			OtherPickABrick(tmp[0],tmp[1],tmp[2]);
			break;
		case MyEventCode.ConfirmInitialPick:
			int[] tmp2=phoEvent.Parameters[ParameterCode.CustomEventContent]as int[];
			ConfirmInitialPick(tmp2[0],tmp2[1]);
			break;
		case MyEventCode.RequestInitialPick:
			int[] tmp3=phoEvent.Parameters[ParameterCode.CustomEventContent]as int[];
			RequestInitialPick(tmp3[0],tmp3[1]);
			break;
		case MyEventCode.ReadyToGo:
			OtherReadyToGo((int)phoEvent.Parameters[ParameterCode.CustomEventContent]);
			break;
		case MyEventCode.GO:
			GO();
			break;
		case MyEventCode.SyncJoker:
			int[] msg=phoEvent.Parameters[ParameterCode.CustomEventContent]as int[];
			SyncJoker(msg[0],msg[1],msg[2],msg[3]);
			break;
		case MyEventCode.OrderOnesBrick:
			OrderOnesBrick((int)phoEvent.Parameters[ParameterCode.CustomEventContent]);
			break;
		case MyEventCode.OtherOpenABrick:
			OtherOpenABrick((int)phoEvent.Parameters[ParameterCode.CustomEventContent]);
			break;
		case MyEventCode.NextOne:
			NextOne((int)phoEvent.Parameters[ParameterCode.CustomEventContent]);
			break;
		case MyEventCode.OneDone:
			OneDone((int)phoEvent.Parameters[ParameterCode.CustomEventContent]);
			break;
		case MyEventCode.GameOver:
			GameOver((int)phoEvent.Parameters[ParameterCode.CustomEventContent]);
			break;
		case MyEventCode.OneLeave:
			OneLeave((int)phoEvent.Parameters[ParameterCode.CustomEventContent]);
			break;
		case MyEventCode.OnePlayAgain:
			OnePlayAgain((int)phoEvent.Parameters[ParameterCode.CustomEventContent]);
		break;
		}
	}
/*
	public void SendEvent(byte eventCode,Hashtable evData,bool sendReliable){
		OpRaiseEvent(eventCode,evData,sendReliable,RaiseEventOptions.Default);
	}*/

}
