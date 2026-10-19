using UnityEngine;

public class Trastion : MonoBehaviour
{
[SerializeField] private RectTransform rectTransform;
   [SerializeField] private Vector2 hiddenPanelLocker = new Vector2(0, 800);
   private Character instace;
   
   
 private Vector2 showPanelLocker;
  bool isOpen=false;

    void Start()
    {
       instace=GetComponent<Character>();
       
       
        showPanelLocker = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = hiddenPanelLocker;
    }

    public void TogglePanelLocker()
    {
        if (!isOpen)
        {
            LeanTween.move(rectTransform, showPanelLocker, 0.4f)
                .setEase(LeanTweenType.easeOutBounce);

     
        }
        else
        {
            LeanTween.move(rectTransform, hiddenPanelLocker, 0.4f)
                .setEase(LeanTweenType.easeInOutQuad);
        }

   isOpen=!isOpen;
    }
    public void rowchangcharacter()
    {
        instace.ChangeCharacter(1);
    }
}