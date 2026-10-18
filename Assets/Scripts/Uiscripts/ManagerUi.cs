using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ManagerUi : MonoBehaviour
{
[SerializeField] private GameObject textplay;
[SerializeField] private GameObject OBimage;
[SerializeField]private Image img;
[SerializeField]private TextMeshProUGUI textui;
private bool isloading=false;
  private float duration=2f;
    

   void Start()
   {
    OBimage.SetActive(false);
    StartCoroutine(fadetext());
   }
  public void BTLocker()
   {
    OBimage.SetActive(true);
    isloading=true;
    StartCoroutine(fadeimage());
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
    IEnumerator fadeimage()
    {
         if(img!=null)
        {
            Color color = img.color;
            float time = 0f;
            while (time<duration)
            {
                
            
            
            color.a=Mathf.Lerp(0f,1f,time/duration);
           img.color = color;
            time +=Time.deltaTime;
            yield return null;
        }
        color.a=1f;
      img.color=color;
SceneManager.LoadScene("LockerUI");

    }
    }
}


