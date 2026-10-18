using UnityEngine;
using UnityEngine.UI;

public class ScrollView : MonoBehaviour
{
  [SerializeField] private Button[] buttons;
  private Image[] images;
 private Character charBT;


    private void Start()
    {
        charBT = FindObjectOfType<Character>();
        for(int i=0; i<buttons.Length; i++)
        {
           int index=i;
           buttons[i].onClick.AddListener(() => charBT.SelectCharacter(index));
        
        }
      
    

    }

  
  
  
}
