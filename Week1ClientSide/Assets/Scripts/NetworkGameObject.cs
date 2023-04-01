using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class NetworkGameObject : MonoBehaviour
{

    [SerializeField] public bool isLocallyOwned;
    [SerializeField] public int uniqueNetworkID;
    [SerializeField] public int localID;
    static int lastAssignedLocalID = -1;
    private void Awake()
    {
       if(isLocallyOwned) localID = lastAssignedLocalID++;
    }

    private void Start()
    {
      
    }

   public byte[] ToPacket() //convert the relevant info on the gameobject to a packet
    {
        //create a delimited string with the required data
        //note if we put strings in this we might want to check they don’t have a semicolon or use a different delimiter like |
        string returnVal = "Object data;" + uniqueNetworkID + ";" +
                            transform.position.x* -100 + ";" +
                            transform.position.z * 100 + ";" +
                            transform.position.y * 100 + ";" +
                            transform.rotation.x + ";" +
                            transform.rotation.z + ";" +
                            transform.rotation.y + ";" +
                            transform.rotation.w + ";"
                            ;
        return Encoding.ASCII.GetBytes(returnVal);
    }
      public void FromPacket(string packet) //convert a packet to the relevant data and apply it to the gameobject properties
    {
        string[] values = packet.Split(';');
        
        
        transform.position = new Vector3(float.Parse(values[2]) / -100, float.Parse(values[4]) / 100, float.Parse(values[3])) / 100;
        transform.rotation = new Quaternion(float.Parse(values[5]), float.Parse(values[7]), float.Parse(values[6]), float.Parse(values[8]));
    }


   

}
