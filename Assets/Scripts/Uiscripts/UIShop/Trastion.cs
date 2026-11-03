using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class Trastion : MonoBehaviour
{
    public static Trastion instance;

    Dictionary<GameObject, Vector2> PanelDic = new();
    [SerializeField] private List<GameObject> panels = new();
    [SerializeField] private GameObject ActivePanel;
    private float duration=0.5f;

    public RectTransform rectTransform;

    private Vector3 showpanel = new Vector2(0, 0);
    private Vector2 hiddenpanel;


    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        for (int i = 0; i < panels.Count; i++)
        {
            if (!PanelDic.ContainsKey(panels[i]))
            {
                RectTransform rect = panels[i].GetComponent<RectTransform>();
                PanelDic[panels[i]] = rect.anchoredPosition;
              
            }
        }
        
    }



    
public void ShowPanel(GameObject gameObject)
{
    
    if (!PanelDic.ContainsKey(gameObject))
        return;

    if (ActivePanel != null && ActivePanel != gameObject)
    {
        LeanTween.cancel(ActivePanel);
        ActivePanel.SetActive(false);
    }


    ActivePanel = gameObject;
    ActivePanel.SetActive(true);

    RectTransform rect = gameObject.GetComponent<RectTransform>();
    rect.DOLocalMove(showpanel,duration).SetEase(Ease.OutBack);

   
}
public void Exit()
{
    if (ActivePanel != null)
    {
        ActivePanel.transform.localPosition=PanelDic[ActivePanel];
        ActivePanel.SetActive(false);
        ActivePanel = null;
    
    }
}
}
