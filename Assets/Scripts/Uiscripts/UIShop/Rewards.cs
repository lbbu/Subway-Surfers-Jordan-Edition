using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class Rewards : MonoBehaviour
{
    [SerializeField] private Button[] rewardButton;
    [SerializeField] private TextMeshProUGUI[] rewardText;
        public List <Item> itemrewards=new();
   [SerializeField] private Image[] imageslocks;

  
  public GameObject panelReward;
    private float timeRemaining;
    
     

    
          
    
    void Start()
    {
       
           if(panelReward != null)
           {
               panelReward.SetActive(true);
           }
               setupobutton();
        
    }
 public void setupobutton()
    {
         for(int i=0; i<rewardButton.Length; i++)
           {
               rewardButton[i].onClick.RemoveAllListeners();
               int index = i;
               
               rewardButton[i].onClick.AddListener(()=>ClaimReward(itemrewards[index], index));
           }
    }
    void Update()
    {
        timeRemaining += Time.deltaTime;
        int hours = Mathf.FloorToInt(timeRemaining / 3600);
        int minutes = Mathf.FloorToInt((timeRemaining % 3600) / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60); 
        rewardText[0].text = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);

        if(hours >= 12)
        {
            int lockhours = hours /12;
          for(int i = 0; i < lockhours && i < imageslocks.Length; i++)
{
    imageslocks[i].enabled = false;
}
        }
        

    }
private void ClaimReward(Item item,int index)
{
        int hours = Mathf.FloorToInt(timeRemaining / 3600);
             int lockhours = (index + 1) * 12;
             if(hours >=lockhours)
             {
                 Inventory.instance.additem(item);
             }
    
  
           

  
}
}