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


[SerializeField]private TextMeshProUGUI textui;
private bool isloading=false;
  private float duration=2f;
    

   void Start()
   {
 
    StartCoroutine(fadetext());
   }
public void ShowPanels(GameObject gameObject)
{
       Debug.Log("Panel name: " + gameObject.name);
    Trastion.instance.ShowPanel(gameObject);


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