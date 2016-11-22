using UnityEngine;
using System.Collections;

/// <summary>
/// 用来实现位移动画的辅助类
/// 记录 起点 终点
/// 含有动画曲线函数
/// 这个类引用了要 使之运动的 GameObject 的Transform
/// 一定要用Init函数初始化这个类
/// </summary>
public class AnimElement :MonoBehaviour{
	bool playing=false;
	Transform _animTarget;//要被驱动的目标的Transform的引用
	SpriteRenderer _animTargetSPR;
	int _animDuration;//动画持续帧数
	float _progress;//动画开始时的进度 0~1  默认从0开始
	Vector3 _fromScale,_toScale;
	Vector3 _fromRotation,_toRotation;
	public float Progress{//用来控制当前实例的生命周期
		get{
			return _progress;
		}
		set{
			_progress=value>=0?value:0;
			_progress=_progress<=1?_progress:1;
		}
	}
	float _currentFrame=0;//当前播放到的帧数,在Init时根据Progress计算得出

	//进行映射的函数
	public delegate float Map(float input);
	Map _mapFunc;

	//不同动画类型的函数  如进行位移动画  进行透明度动画
	delegate void AnimFunc();
	AnimFunc _animFunc;

	//动画播放完成的事件
	public delegate void PostAnimationEventHandler();
	public event PostAnimationEventHandler PostAnimation;

	public Map MapFunc{
		get{
			return _mapFunc;
		}
		set{
			_mapFunc=value;
		}
	}
	public Vector3 startPos;
	public Vector3 endPos;
	public float beginAlpha,endAlpha;


	public static AnimElement Prepare(){
		return new GameObject().AddComponent<AnimElement>();
	}

	// input range from 0---1
	float DefaultMapFunc(float input){
		return Mathf.Sin(input*Mathf.PI/2);
		//return input;
	}
	//局部坐标动画
	public AnimElement InitLocal(Vector3 localBegin,Vector3 localEnd,Transform localTrans,
		float progress,int duration=60){
		startPos=localBegin;
		endPos=localEnd;
		_animTarget=localTrans;
		Progress=progress;
		_currentFrame=Progress*duration;
		_animDuration=duration;
		MapFunc=DefaultMapFunc;
		_animFunc=Anim_LocalMover;
		//返回自己,方便这种用法 xxx.Init().Play();
		return this;
	}
	//UGUI坐标动画
	public AnimElement InitUGUI(Vector3 begin,Vector3 end,Transform trans,
		float progress,int duration=60){
		return InitLocal(begin,end,trans,progress);
	}
	//世界坐标动画
	public AnimElement Init(Vector3 begin,Vector3 end,Transform trans,
	                 float progress,int duration=60){
		startPos=begin;
		endPos=end;
		_animTarget=trans;
		Progress=progress;
		_currentFrame=Progress*duration;
		_animDuration=duration;
		MapFunc=DefaultMapFunc;
		_animFunc=Anim_Mover;
		//返回自己,方便这种用法 xxx.Init().Play();
		return this;
	}
	//颜色动画
	public AnimElement Init(Color begin,Color end,Transform trans,
		float progressLR,int duration=60){
		startPos=new Vector3(begin.r,begin.g,begin.b);
		endPos=new Vector3(end.r,end.g,end.b);
		beginAlpha=begin.a;
		endAlpha=end.a;
		_animTargetSPR=trans.GetComponent<SpriteRenderer>();
		Progress=progressLR;
		_currentFrame=Progress*duration;
		_animDuration=duration;
		MapFunc=DefaultMapFunc;
		_animFunc=Anim_Color;
		//返回自己,方便这种用法 xxx.Init().Play();
		return this;
	}
	//添加localScale动画
	public AnimElement AddScaleAnimation(Vector3 from,Vector3 to){
		_fromScale=from;
		_toScale=to;
		_animFunc+=Anim_LocalScale;
		return this;
	}
	//localRotate动画
	public AnimElement AddLocalRotateAnimation(Vector3 from,Vector3 to){
		_fromRotation=from;
		_toRotation=to;
		_animFunc+=Anim_LocalRotate;
		return this;
	}
	//是否需要反向播放动画
	public AnimElement SetReverseAnimation(bool reverse){
		if(reverse){
			var tmp=_toScale;
			_toScale=_fromScale;
			_fromScale=tmp;

			var tmp2=startPos;
			startPos=endPos;
			endPos=tmp2;

			var tmp3=_fromRotation;
			_fromRotation=_toRotation;
			_toRotation=tmp3;

			var tmp4=beginAlpha;
			beginAlpha=endAlpha;
			endAlpha=tmp4;
		}
		return this;
	}
	public void Play(){
		playing=true;
	}



	#region  动画函数
	void Anim_LocalRotate(){
		float mapped=MapFunc(Progress);
		_animTarget.localRotation=Quaternion.Euler(Tool.Interpolate(_fromRotation,_toRotation,mapped));
	}
	void Anim_LocalScale(){
		float mapped=MapFunc(Progress);
		_animTarget.localScale=Tool.Interpolate(_fromScale,_toScale,mapped);
	}
	void Anim_LocalMover(){
		float mapped=MapFunc(Progress);
		_animTarget.localPosition=Tool.Interpolate(startPos,endPos,mapped);
	}
	void Anim_Mover(){
		float mapped=MapFunc(Progress);
		_animTarget.position=Tool.Interpolate(startPos,endPos,mapped);
	}
	void Anim_Color(){
		float mappedProgress=MapFunc(Progress);
		Vector3 tmpVec=Tool.Interpolate(startPos,endPos,mappedProgress);
		_animTargetSPR.color=new Color(tmpVec.x,tmpVec.y,tmpVec.z,Mathf.Lerp(beginAlpha,endAlpha,mappedProgress));
	}
	#endregion



	void Start(){
	}

	//动画播放完后执行的函数
	public AnimElement OnFinish(PostAnimationEventHandler  func){
		PostAnimation+=func;
		return this;	
	}

	void Update(){
		//只有完成了init的实例才进行这里面的操作
		if(playing){
		//动画完成后销毁本实例
		if(Progress>=1){
				if(PostAnimation!=null){
					PostAnimation();
				}
			Destroy(transform.gameObject);
		}
		_currentFrame++;
		Progress=_currentFrame/(float)_animDuration;

		//用delegate动态调用不同的动画函数
		_animFunc();
	}
	}
}
