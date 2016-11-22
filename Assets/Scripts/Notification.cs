using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using DG.Tweening;

public class Notification : MonoBehaviour {

    public RectTransform ntfPanel;
    float panelWidth = 800  ;//面板容器大小
    float panelHeight = 200 ;
    float canvasWidth = 480;//标准canvas
    float canvasHeight = 300;
    float nW, nH;//缩放因子
    public float timer = 0f;
    
    //lt和rb分别为矩形左上角和右下角，existTime为停留时间
    public void autoNtf(string msg, Vector2 lt, Vector2 rb ,float existTime)
    {
        timer = existTime;
        nW = Mathf.Abs(rb.x - lt.x) * canvasWidth / (Screen.width * panelWidth);                   ///////////////////////////////////////////
        nH = (rb.y - lt.y) * canvasHeight / (Screen.height * panelHeight);
        ntfPanel.position = new Vector3(lt.x*Screen.width
									, ntfPanel.position.y, ntfPanel.position.z);
		nW=rb.x-lt.x;
		nH=rb.y-lt.y;
        //ntfPanel.GetChild(0).localScale = new Vector3(nW, nH, 1);
		//ntfPanel.position=new Vector2(lt.x*Screen.width,lt.y*Screen.height);
		//ntfPanel.position=new Vector2(lt.x*Screen.width,lt.y*Screen.height);

		//ntfPanel.GetComponent<RectTransform>().rect.height=Screen.height*nH;

		ntfPanel.DOMoveY(Screen.height*0.7f, 0.4f).SetEase(Ease.OutBack);
        ntfPanel.GetComponentInChildren<Text>().text = msg;

    }

    public void dropNtf(string msg, Vector2 lt, Vector2 rb)
    {
        nW = Mathf.Abs(rb.x - lt.x) * (canvasWidth / Screen.width) / panelWidth;                   ///////////////////////////////////////////
        nH = (rb.y - lt.y) * (canvasHeight / Screen.height) / panelHeight;
        ntfPanel.position = new Vector3(lt.x, ntfPanel.position.y, ntfPanel.position.z);
        ntfPanel.localScale=new Vector3(nW,nH,1);
        ntfPanel.DOMoveY(Screen.height - lt.y, 0.4f).SetEase(Ease.OutBack);
        ntfPanel.GetComponentInChildren<Text>().text = msg;
    }

    public void riseNtf()
    {
        ntfPanel.transform.DOMoveY(0, 0.4f).SetEase(Ease.OutBack);
    }

    void Update()
    {
        if(timer>0f) timer -= Time.deltaTime;
        if (timer < 0)
        {
            riseNtf();
            timer = 0f;
        }
    }

}
