using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.VisualScripting;
using UnityEngine.UIElements;

public class ManagerUi : MonoBehaviour
{
[SerializeField] private GameObject textplay;
bool isopen;



[SerializeField]private TextMeshProUGUI textui;

  private float duration=2f;
    

   void Start()
   {
 
    StartCoroutine(fadetext());
   }
public void ShowPanels(GameObject panel)
{
  

    if (!isopen)
    {
        Trastion.instance.ShowPanel(panel);
       
    }
    else
    {
       hidpanel();
       
    }
isopen = !isopen;

}
   
public void hidpanel()
    {
       
        
          Trastion.instance.Exit();
      
        
     
    }



     IEnumerator fadetext()
    {
        while(true)
        {
        float time=0f;
        if(textui!=null)
        {
            Color color = textui.color;
            while (time<duration)
            {
                
            
            
            color.a=Mathf.Lerp(1f,0f,time/duration);
           textui.color = color;
            time +=Time.deltaTime;
            yield return null;
        }
        color.a=0;
      textui.color=color;

    }
        }
    }
    
}