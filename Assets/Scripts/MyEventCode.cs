
// 1----199都是可以用的
public class MyEventCode{
	public  const byte SyncAllBricks=21;
	public const byte OtherPickABrick=22;
	public const byte ConfirmInitialPick=23;
	public const byte RequestInitialPick=24;
	public const byte ReadyToGo=25;//我已经整理完毕
	public const byte GO=26;//游戏正式开始
	public const byte SyncJoker=27;// 同步joker在数组中的位置
	public const byte OrderOnesBrick=28;// 对某个玩家的牌进行排序
	public const byte OtherOpenABrick=29;// open某个玩家的牌
	public const byte NextOne=30;// 轮到下一个玩家
	public const byte GameOver=31;//游戏结束
	public const byte  OneDone=32;//一个玩家的所有牌都翻开了
	public const byte OneLeave=33;
	public const byte OnePlayAgain=34;//玩家 id 选择再来一局
}