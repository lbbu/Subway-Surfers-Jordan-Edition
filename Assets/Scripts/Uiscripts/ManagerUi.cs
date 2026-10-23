using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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