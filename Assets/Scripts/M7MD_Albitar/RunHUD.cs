using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>واجهة الجولة قابلة للمس، وتعرض الصحة والنتائج وتحترم المساحة الآمنة للهاتف.</summary>
public sealed class RunHUD : MonoBehaviour
{
    [SerializeField] private RunSession session;
    [SerializeField] private RunScore score;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private RunnerSettings settings;
    private RectTransform safe;
    private Text stats, title, details, primaryLabel, soundLabel, shakeLabel, hapticsLabel;
    private GameObject modal, restart;
    private Button pause;
    private RectTransform recovery;
    private Font font;
    private void Start() => Build();
    // العناصر تخص هذه النسخة من المشهد فقط؛ يمكن استبدال تصميمها لاحقًا دون تغيير دورة الجولة.
    private void Build()
    {
        if (safe) return;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var root = new GameObject("Runner HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080,1920); scaler.matchWidthOrHeight = .5f;
        safe = Panel(root.transform,"Safe Area",Vector2.zero,Vector2.one,Color.clear); safe.GetComponent<Image>().raycastTarget=false;
        var top=Panel(safe,"Run stats",new Vector2(0,.88f),Vector2.one,new Color(.025f,.05f,.1f,.85f));
        recovery=Panel(top,"Recovery",new Vector2(.05f,.02f),new Vector2(.73f,.055f),new Color(.2f,.9f,.6f)); recovery.GetComponent<Image>().raycastTarget=false;
        stats=Label(top,"",new Vector2(.04f,0),new Vector2(.76f,1),35);
        pause=Button(top,"II",new Vector2(.8f,.2f),new Vector2(.96f,.8f),()=>session.PauseRun());
        modal=Panel(safe,"Menu",Vector2.zero,Vector2.one,new Color(0,0,0,.65f)).gameObject;
        var card=Panel(modal.transform,"Card",new Vector2(.07f,.22f),new Vector2(.93f,.78f),new Color(.04f,.09f,.16f,.98f));
        title=Label(card,"JORDAN RUNNER",new Vector2(.05f,.8f),new Vector2(.95f,.97f),55);
        details=Label(card,"",new Vector2(.05f,.57f),new Vector2(.95f,.8f),32);
        var primary=Button(card,"RUN",new Vector2(.08f,.43f),new Vector2(.92f,.55f),Primary); primaryLabel=primary.GetComponentInChildren<Text>();
        restart=Button(card,"RESTART",new Vector2(.08f,.29f),new Vector2(.92f,.4f),()=>session.RestartRun()).gameObject;
        soundLabel=Button(card,"",new Vector2(.04f,.14f),new Vector2(.34f,.25f),()=>settings.ToggleSound()).GetComponentInChildren<Text>();
        shakeLabel=Button(card,"",new Vector2(.36f,.14f),new Vector2(.66f,.25f),()=>settings.ToggleShake()).GetComponentInChildren<Text>();
        hapticsLabel=Button(card,"",new Vector2(.68f,.14f),new Vector2(.98f,.25f),()=>settings.ToggleHaptics()).GetComponentInChildren<Text>();
        Label(card,"Swipe to move / jump / slide",new Vector2(.04f,.02f),new Vector2(.96f,.12f),25);
        if (!FindFirstObjectByType<EventSystem>())
        {
            var events=new GameObject("Runner UI Input",typeof(EventSystem)); events.transform.SetParent(transform,false);
#if ENABLE_INPUT_SYSTEM
            events.AddComponent<InputSystemUIInputModule>();
#else
            events.AddComponent<StandaloneInputModule>();
#endif
        }
    }
    private void Primary() { if(session.State==RunSession.RunState.Ready) session.StartRun(); else if(session.State==RunSession.RunState.Paused) session.ResumeRun(); else session.RestartRun(); }
    private void Update()
    {
        if(!safe || !session || !score || !health || !settings) return;
        // بعض أجهزة المحاكاة تعيد أبعاد شاشة الجهاز بدل نافذة اللعبة؛ نستخدم الشاشة كاملة عند عدم تطابقها.
        Rect a=Screen.safeArea; if(a.width<=0 || a.height<=0 || a.xMax>Screen.width+1 || a.yMax>Screen.height+1) a=new Rect(0,0,Screen.width,Screen.height); safe.anchorMin=new Vector2(a.x/Screen.width,a.y/Screen.height); safe.anchorMax=new Vector2(a.xMax/Screen.width,a.yMax/Screen.height);
        stats.text=$"SCORE  {score.Points:000000}\n{score.Distance:0} m     COINS  {score.Coins}\nLIVES  {health.Hearts}/2" + (health.Hearts==1 ? $"    +1 in {health.RecoveryRemaining:0.0}s" : "");
        recovery.gameObject.SetActive(health.Hearts==1); recovery.anchorMax=new Vector2(.05f+.68f*(1f-health.RecoveryRemaining/Mathf.Max(.01f,health.RecoveryDuration)),.055f);
        var state=session.State; bool playing=state==RunSession.RunState.Playing; modal.SetActive(!playing); pause.gameObject.SetActive(playing);
        if(playing)return;
        title.text=state==RunSession.RunState.Ready?"JORDAN RUNNER":state==RunSession.RunState.Paused?"PAUSED":"RUN COMPLETE";
        details.text=$"BEST  {score.Best}\nSCORE  {score.Points}    COINS  {score.Coins}\nDISTANCE  {score.Distance:0} m";
        primaryLabel.text=state==RunSession.RunState.Ready?"RUN":state==RunSession.RunState.Paused?"RESUME":"TRY AGAIN";
        restart.SetActive(state==RunSession.RunState.Paused);
        soundLabel.text="SOUND\n"+(settings.Sound?"ON":"OFF"); shakeLabel.text="SHAKE\n"+(settings.Shake?"ON":"OFF"); hapticsLabel.text="HAPTICS\n"+(settings.Haptics?"ON":"OFF");
    }
    private RectTransform Panel(Transform parent,string name,Vector2 min,Vector2 max,Color color)
    { var go=new GameObject(name,typeof(RectTransform),typeof(Image)); var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;go.GetComponent<Image>().color=color;return r; }
    private Text Label(Transform parent,string text,Vector2 min,Vector2 max,int size)
    { var r=Panel(parent,"Label",min,max,Color.clear);r.GetComponent<Image>().raycastTarget=false;var t=new GameObject("Text",typeof(RectTransform),typeof(Text)).GetComponent<Text>();var tr=t.rectTransform;tr.SetParent(r,false);tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;t.font=font;t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.color=Color.white;t.raycastTarget=false;t.text=text;return t; }
    private Button Button(Transform parent,string text,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action)
    {var r=Panel(parent,text,min,max,new Color(.78f,.32f,.055f,1));var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();b.onClick.AddListener(action);Label(r,text,Vector2.zero,Vector2.one,29);return b;}
}
